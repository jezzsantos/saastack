import * as path from "node:path";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";
import {ISettingsFile, SettingsFile} from "./settingsFile";
import {IAppSettingsJsonFileReader} from "./appSettingsJsonFileReader";
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
            set.accumulateVariables();
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

    verifyConfiguration(gitHubVariables: any, gitHubSecrets: any): boolean {
        if (this.sets.length === 0) {
            return true;
        }

        this.logger.info("Verifying of all settings files, in all hosts");
        let isAllSetsVerified = true;
        for (const set of this.sets) {
            const isSetVerified = set.verify(this.logger, gitHubVariables, gitHubSecrets);
            if (!isSetVerified) {
                isAllSetsVerified = false;
            }
        }

        if (!isAllSetsVerified) {
            this.logger.error("Verification of all settings files, in all hosts: -> Failed! there are missing required variables in at least one of the hosts. See errors above");
        } else {
            this.logger.info("Verification of all settings files, in all hosts: -> Successful!");
        }

        return isAllSetsVerified;
    }

    substituteVariables(gitHubVariables: any, gitHubSecrets: any) {

        //TODO: Substitute: walk each configuration set, for each settings file:
        // 1. substitute the variables with the values from the variables/secrets (in-memory), then
        // 2. write those (in-memory) files to disk (in their original locations). 

    }

}