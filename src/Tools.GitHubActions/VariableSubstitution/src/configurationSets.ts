import * as path from "node:path";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";
import {ISettingsFile, SettingsFile} from "./settingsFile";
import {IAppSettingsReaderWriterFactory} from "./appSettingsReaderWriterFactory";
import {ConfigurationSet, IConfigurationSet} from "./configurationSet";
import {WarningOptions} from "./main";
import {GitHubVariables} from "./settingsFileProcessor";

export class ConfigurationSets {
    _sets: IConfigurationSet[] = [];
    private readonly _logger: ILogger;
    private readonly _warningOptions: WarningOptions;

    private constructor(logger: ILogger, sets: IConfigurationSet[], warningOptions: WarningOptions) {
        this._sets = sets;
        this._logger = logger;
        this._warningOptions = warningOptions;
    }

    get hasNone(): boolean {
        return this._sets.length === 0;
    }

    get length(): number {
        return this._sets.length;
    }

    public static async create(logger: ILogger, globParser: IGlobPatternParser, readerWriterFactory: IAppSettingsReaderWriterFactory, globPattern: string, warningOptions: WarningOptions): Promise<ConfigurationSets> {
        const matches = globPattern.length > 0 ? globPattern.split(',') : [];

        const files = await globParser.parseFiles(matches);
        if (files.length === 0) {
            logger.warning(ConfigurationSetsMessages.noSettingsFound(globPattern));
            return new ConfigurationSets(logger, [], warningOptions);
        }

        const sets: ConfigurationSet[] = [];
        for (const file of files) {
            await ConfigurationSets.accumulateFilesIntoSets(logger, readerWriterFactory, sets, file);
        }

        for (const set of sets) {
            set.accumulateVariables();
        }

        logger.info(ConfigurationSetsMessages.foundSettingsFiles(sets));

        return new ConfigurationSets(logger, sets, warningOptions);
    }

    private static async accumulateFilesIntoSets(logger: ILogger, readerWriterFactory: IAppSettingsReaderWriterFactory, sets: ConfigurationSet[], file: string) {

        const hostProjectPath: string = path.dirname(file);

        const set = sets.find(set => set.hostProjectPath.includes(hostProjectPath));
        if (set) {
            const setting = await SettingsFile.create(logger, readerWriterFactory, file);
            set.settingFiles.push(setting);

        } else {
            const setting = await SettingsFile.create(logger, readerWriterFactory, file);
            const settingFiles: ISettingsFile[] = [setting];
            sets.push(new ConfigurationSet(hostProjectPath, settingFiles));
        }
    }

    verifyConfiguration(gitHubVariables: any, gitHubSecrets: any): boolean {
        if (this._sets.length === 0) {
            return true;
        }

        this._logger.info(ConfigurationSetsMessages.startVerification());
        let isAllSetsVerified = true;
        for (const set of this._sets) {
            const isSetVerified = set.verify(this._logger, gitHubVariables, gitHubSecrets);
            if (!isSetVerified) {
                isAllSetsVerified = false;
            }
        }

        if (!isAllSetsVerified) {
            this._logger.error(ConfigurationSetsMessages.verificationFailed());
        } else {
            this._logger.info(ConfigurationSetsMessages.verificationSucceeded());
        }

        this.verifyAdditionalVariables(gitHubVariables, gitHubSecrets);

        return isAllSetsVerified;
    }

    public verifyAdditionalVariables(gitHubVariables: any, gitHubSecrets: any) {

        if (this._warningOptions.warnOnAdditionalVariables) {
            const allSeenVariables = [...new Set(this._sets.map(set => set.definedVariables.map(name => GitHubVariables.calculateVariableOrSecretName(name))).flat())];
            const allAvailableVariables = [...new Set(Object.keys(gitHubVariables).concat(Object.keys(gitHubSecrets)))];

            let additionalAvailableVariables = allAvailableVariables
                .filter(variable => variable.toLowerCase() !== 'github_token')
                .filter(variable => !allSeenVariables.includes(variable));
            if (additionalAvailableVariables.length > 0) {
                if (this._warningOptions.ignoreAdditionalVariableExpression.length > 0) {
                    additionalAvailableVariables = additionalAvailableVariables
                        .filter(variable => {
                            const match = new RegExp(this._warningOptions.ignoreAdditionalVariableExpression).exec(variable);
                            return match == null
                        });
                }

                if (additionalAvailableVariables.length > 0) {
                    this._logger.warning(ConfigurationSetsMessages.additionalVariablesUnused(additionalAvailableVariables));
                }
            }
        }
    }

    async substituteVariables(gitHubVariables: any, gitHubSecrets: any): Promise<boolean> {
        if (this._sets.length === 0) {
            return true;
        }

        this._logger.info(ConfigurationSetsMessages.startSubstitution());
        let isAllSetsSubstituted = true;
        for (const set of this._sets) {
            const isSetSubstituted = await set.substitute(this._logger, this._warningOptions, gitHubVariables, gitHubSecrets);
            if (!isSetSubstituted) {
                isAllSetsSubstituted = false;
            }
        }

        if (!isAllSetsSubstituted) {
            this._logger.error(ConfigurationSetsMessages.substitutionFailed());
        } else {
            this._logger.info(ConfigurationSetsMessages.substitutionSucceeded());
        }

        return isAllSetsSubstituted;
    }
}


export class ConfigurationSetsMessages {
    public static noSettingsFound = (globPattern: string): string => `No settings files found in this repository, applying the glob patterns: ${globPattern}`;
    public static foundSettingsFiles = (sets: IConfigurationSet[]): string => {
        const allFiles = sets.map(set => `${set.hostProjectPath}:\n\t\t${set.settingFiles.map(file => path.basename(file.path)).join(',\n\t\t')}`).join(',\n\t');
        return `Found settings files, in these hosts:\n\t${allFiles}`
    };
    public static startVerification = (): string => "Verifying of all settings files, in all hosts";
    public static verificationFailed = (): string => "Verification of all settings files, in all hosts: -> Failed! there are missing required variables in at least one of the hosts. See errors above";
    public static verificationSucceeded = (): string => "Verification of all settings files, in all hosts: -> Successful!";
    public static startSubstitution = (): string => "Substituting all settings in all settings files, in all hosts";
    public static substitutionFailed = (): string => "Substitution of all settings in all settings files, in all hosts: -> Failed! See errors above";
    public static substitutionSucceeded = (): string => "Substitution of all settings in all settings files, in all hosts: -> Successful!";
    public static additionalVariablesUnused = (additionalVariables: string[]): string => {
        const count = additionalVariables.length;
        const variables = additionalVariables.join(', \n\t');
        return `The following '${count}' GitHub variables or secrets have been defined in this repository, but they will not to be substituted in any of the settings files, in any of the hosts:\n\t${variables}`;
    };
}