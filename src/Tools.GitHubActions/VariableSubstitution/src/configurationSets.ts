import * as path from "node:path";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";

interface ConfigurationSet {
    HostProjectPath: string;
    SettingFiles: string[];
    RequiredVariables: string[];
}

export class ConfigurationSets {
    sets: ConfigurationSet[] = [];

    private constructor(sets: ConfigurationSet[]) {
        this.sets = sets;
    }

    get hasNone(): boolean {
        return this.sets.length === 0;
    }

    get length(): number {
        return this.sets.length;
    }

    public static async create(logger: ILogger, globParser: IGlobPatternParser, globPattern: string): Promise<ConfigurationSets> {
        const matches = globPattern.length > 0 ? globPattern.split(',') : [];

        const files = await globParser.parseFiles(matches);
        if (files.length === 0) {
            logger.warning(`No settings files found in this repository, using the glob patterns: ${globPattern}`);
            return new ConfigurationSets([]);
        }

        const sets: ConfigurationSet[] = [];
        files.forEach(file => {
            const hostProjectPath: string = path.dirname(file);
            const requiredVariables: string[] = []; //TODO: we need to harvest the required properties from the JSON file (if they exist)

            const set = sets.find(x => x.HostProjectPath.includes(hostProjectPath));
            if (set) {
                set.SettingFiles.push(file);
            } else {
                const settingFiles: string[] = [file];
                sets.push({
                    HostProjectPath: hostProjectPath,
                    SettingFiles: settingFiles,
                    RequiredVariables: requiredVariables
                });
            }
        });


        const allFiles = sets.map(set => `${set.HostProjectPath}:\n\t\t${set.SettingFiles.map(file => path.basename(file)).join(',\n\t\t')}`).join(',\n\t');
        logger.info(`Found settings files:\n\t${allFiles}`);

        return new ConfigurationSets(sets);
    }
}