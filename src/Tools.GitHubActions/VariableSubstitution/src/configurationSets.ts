import * as path from "node:path";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";
import {ISettingsFile, SettingsFile} from "./settingsFile";
import {IAppSettingsJsonFileReader} from "./appSettingsJsonFileReader";

interface IConfigurationSet {
    readonly hostProjectPath: string;
    readonly settingFiles: ISettingsFile[];
    readonly requiredVariables: string[];
    readonly definedVariables: string[];

    accumulateRequiredVariables(variables: string[]): void;

    accumulateDefinedVariables(variables: string[]): void;
}

class ConfigurationSet implements IConfigurationSet {
    constructor(hostProjectPath: string, settingFiles: ISettingsFile[]) {
        this._hostProjectPath = hostProjectPath;
        this._settingFiles = settingFiles;
        this._requiredVariables = [];
        this._definedVariables = [];
    }

    _hostProjectPath: string;

    get hostProjectPath(): string {
        return this._hostProjectPath;
    }

    _settingFiles: ISettingsFile[];

    get settingFiles(): ISettingsFile[] {
        return this._settingFiles;
    }

    _requiredVariables: string[];

    get requiredVariables(): string[] {
        return this._requiredVariables;
    }

    _definedVariables: string[];

    get definedVariables(): string[] {
        return this._definedVariables;
    }

    accumulateRequiredVariables(variables: string[]) {
        for (const variable of variables) {
            if (!this._requiredVariables.includes(variable)) {
                this._requiredVariables.push(variable);
            }
        }
    }

    accumulateDefinedVariables(variables: string[]) {
        for (const variable of variables) {
            if (!this._definedVariables.includes(variable)) {
                this._definedVariables.push(variable);
            }
        }
    }
}


export class ConfigurationSets {
    sets: IConfigurationSet[] = [];
    private logger: ILogger;

    private constructor(logger: ILogger, sets: IConfigurationSet[]) {
        this.sets = sets;
        this.logger = logger;
    }

    get hasNone(): boolean {
        return this.sets.length === 0;
    }

    get length(): number {
        return this.sets.length;
    }

    public static async create(logger: ILogger, globParser: IGlobPatternParser, jsonFileReader: IAppSettingsJsonFileReader, globPattern: string): Promise<ConfigurationSets> {
        const matches = globPattern.length > 0 ? globPattern.split(',') : [];

        const files = await globParser.parseFiles(matches);
        if (files.length === 0) {
            logger.warning(`No settings files found in this repository, applying the glob patterns: ${globPattern}`);
            return new ConfigurationSets(logger, []);
        }

        const sets: ConfigurationSet[] = [];
        for (const file of files) {
            await ConfigurationSets.accumulateFilesIntoSets(jsonFileReader, sets, file);
        }

        for (const set of sets) {
            ConfigurationSets.accumulateAllVariablesForSet(set);
        }

        const allFiles = sets.map(set => `${set.hostProjectPath}:\n\t\t${set.settingFiles.map(file => path.basename(file.path)).join(',\n\t\t')}`).join(',\n\t');
        logger.info(`Found settings files, in these hosts:\n\t${allFiles}`);

        return new ConfigurationSets(logger, sets);
    }

    private static async accumulateFilesIntoSets(jsonFileReader: IAppSettingsJsonFileReader, sets: ConfigurationSet[], file: string) {

        const hostProjectPath: string = path.dirname(file);

        const set = sets.find(set => set.hostProjectPath.includes(hostProjectPath));
        if (set) {
            const setting = await SettingsFile.create(jsonFileReader, file);
            set.settingFiles.push(setting);

        } else {
            const setting = await SettingsFile.create(jsonFileReader, file);
            const settingFiles: ISettingsFile[] = [setting];
            sets.push(new ConfigurationSet(hostProjectPath, settingFiles));
        }
    }

    private static accumulateAllVariablesForSet(set: ConfigurationSet) {

        const files = set.settingFiles;
        for (const file of files) {
            set.accumulateDefinedVariables(file.variables);
            if (file.hasRequired) {
                set.accumulateRequiredVariables(file.requiredVariables);
            }
        }
    }

    verifyConfiguration(gitHubVariables: any, gitHubSecrets: any): boolean {
        if (this.sets.length === 0) {
            return true;
        }

        let isAllSetsVerified = true;
        for (const set of this.sets) {
            this.logger.info(`Verifying settings files in host: '${set.hostProjectPath}'`);
            let isSetVerified = true;
            for (const requiredVariable of set.requiredVariables) {
                if (!set.definedVariables.includes(requiredVariable)) {
                    this.logger.warning(`Required variable '${requiredVariable}' is not yet defined in any of the settings files of this host! Consider either defining it in one of the settings files of this host, OR remove it from the '${SettingsFile.RequiredProperty}' section of the settings files in this host`);
                } else {
                    const gitHubVariableName = this.calculateGitHubVariableName(requiredVariable);
                    if (!this.isDefinedInGitHubVariables(gitHubVariables, gitHubSecrets, gitHubVariableName)) {
                        isSetVerified = false;
                        this.logger.error(`Required GitHub environment variable (or secret) '${gitHubVariableName}' (alias: '${requiredVariable}') has not been defined in the environment variables (or secrets) of this GitHub project!`);
                    }
                }
            }

            if (!isSetVerified) {
                this.logger.error(`Verification settings files in host: '${set.hostProjectPath}' -> Failed! there is at least one missing required environment variable or secret in this GitHub project`);
                isAllSetsVerified = false;
            } else {
                this.logger.info(`Verifying settings files in host: '${set.hostProjectPath}' -> Successful!`);
            }

        }

        if (!isAllSetsVerified) {
            this.logger.error("Verification of all settings files, in all hosts: -> Failed! there are missing required variables in at least one of the hosts. See errors above");
        } else {
            this.logger.info("Verification of all settings files, in all hosts: -> Successful!");
        }

        return isAllSetsVerified;
    }

    private isDefinedInGitHubVariables(gitHubVariables: any, gitHubSecrets: any, name: string): boolean {

        return gitHubVariables.hasOwnProperty(name) || gitHubSecrets.hasOwnProperty(name);
    }

    private calculateGitHubVariableName(requiredVariable: string) {
        return requiredVariable
            .toUpperCase()
            .replace(/[-:.]/g, '_');
    }
}