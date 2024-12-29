import * as path from "node:path";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";
import {ISettingsFile, SettingsFile} from "./settingsFile";
import {IAppSettingsJsonFileReaderWriter} from "./appSettingsJsonFileReaderWriter";
import {ConfigurationSet, IConfigurationSet} from "./configurationSet";

export class ConfigurationSets {
    sets: IConfigurationSet[] = [];
    private readonly logger: ILogger;

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

    public static async create(logger: ILogger, globParser: IGlobPatternParser, jsonFileReader: IAppSettingsJsonFileReaderWriter, globPattern: string): Promise<ConfigurationSets> {
        const matches = globPattern.length > 0 ? globPattern.split(',') : [];

        const files = await globParser.parseFiles(matches);
        if (files.length === 0) {
            logger.warning(ConfigurationSetsMessages.noSettingsFound(globPattern));
            return new ConfigurationSets(logger, []);
        }

        const sets: ConfigurationSet[] = [];
        for (const file of files) {
            await ConfigurationSets.accumulateFilesIntoSets(jsonFileReader, sets, file);
        }

        for (const set of sets) {
            set.accumulateVariables();
        }

        logger.info(ConfigurationSetsMessages.foundSettingsFiles(sets));

        return new ConfigurationSets(logger, sets);
    }

    private static async accumulateFilesIntoSets(jsonFileReader: IAppSettingsJsonFileReaderWriter, sets: ConfigurationSet[], file: string) {

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

    verifyConfiguration(gitHubVariables: any, gitHubSecrets: any): boolean {
        if (this.sets.length === 0) {
            return true;
        }

        this.logger.info(ConfigurationSetsMessages.startVerification());
        let isAllSetsVerified = true;
        for (const set of this.sets) {
            const isSetVerified = set.verify(this.logger, gitHubVariables, gitHubSecrets);
            if (!isSetVerified) {
                isAllSetsVerified = false;
            }
        }

        if (!isAllSetsVerified) {
            this.logger.error(ConfigurationSetsMessages.verificationFailed());
        } else {
            this.logger.info(ConfigurationSetsMessages.verificationSucceeded());
        }

        return isAllSetsVerified;
    }

    async substituteVariables(gitHubVariables: any, gitHubSecrets: any): Promise<boolean> {
        if (this.sets.length === 0) {
            return true;
        }

        this.logger.info(ConfigurationSetsMessages.startSubstitution());
        let isAllSetsSubstituted = true;
        for (const set of this.sets) {
            const isSetSubstituted = await set.substitute(this.logger, gitHubVariables, gitHubSecrets);
            if (!isSetSubstituted) {
                isAllSetsSubstituted = false;
            }
        }

        if (!isAllSetsSubstituted) {
            this.logger.error(ConfigurationSetsMessages.substitutionFailed());
        } else {
            this.logger.info(ConfigurationSetsMessages.substitutionSucceeded());
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
}