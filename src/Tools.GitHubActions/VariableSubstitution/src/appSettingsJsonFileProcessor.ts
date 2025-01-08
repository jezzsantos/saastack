import {ILogger} from "./logger";
import {IFileReaderWriter} from "./fileReaderWriter";
import {SettingsFile} from "./settingsFile";
import {
    AppSettingRequiredVariable,
    AppSettingVariables,
    GitHubVariables,
    ISettingsFileProcessor,
    SettingsFileProcessorMessages
} from "./settingsFileProcessor";

export class AppSettingsJsonFileProcessor implements ISettingsFileProcessor {
    _logger: ILogger;
    _path: string;
    _readerWriter: IFileReaderWriter;

    constructor(logger: ILogger, readerWriter: IFileReaderWriter, path: string) {
        this._logger = logger;
        this._path = path;
        this._readerWriter = readerWriter;
    }

    private static accumulateVariablesRecursively(logger: ILogger, json: any, variables: string[], requiredVariables: AppSettingRequiredVariable[], prefix: string = "") {
        for (const key in json) {
            if (json.hasOwnProperty(key)) {
                const element = json[key];
                const fullyQualifiedVariableName = AppSettingsJsonFileProcessor.calculateFullyQualifiedVariableName(prefix, key);
                if (typeof element === "object") {
                    if (AppSettingsJsonFileProcessor.isDeployRequiredKey(element, key, prefix)) {
                        const required = AppSettingsJsonFileProcessor.getDeployRequiredVariables(element);
                        if (required.length > 0) {
                            requiredVariables.push(...required);
                        }
                    } else {
                        AppSettingsJsonFileProcessor.accumulateVariablesRecursively(logger, element, variables, requiredVariables, fullyQualifiedVariableName);
                    }
                } else {
                    variables.push(fullyQualifiedVariableName);
                }
            }
        }
    }

    private static substituteVariablesRecursively(logger: ILogger, gitHubVariables: any, gitHubSecrets: any, json: any, prefix: string = "") {
        for (const key in json) {
            if (json.hasOwnProperty(key)) {
                const element = json[key];
                const fullyQualifiedVariableName = AppSettingsJsonFileProcessor.calculateFullyQualifiedVariableName(prefix, key);
                if (typeof element === "object") {
                    let deployKey = AppSettingsJsonFileProcessor.getDeployRequiredKey(element, key, prefix);
                    if (deployKey) {
                        json[key] = SettingsFileProcessorMessages.redactedDeployMessage();
                    } else {
                        AppSettingsJsonFileProcessor.substituteVariablesRecursively(logger, gitHubVariables, gitHubSecrets, element, fullyQualifiedVariableName);
                    }
                } else {
                    if (typeof element === "string" || typeof element === "number" || typeof element === "boolean") {
                        const githubVariableName = GitHubVariables.calculateVariableOrSecretName(fullyQualifiedVariableName);
                        const gitHubSecretOrVariableValue = GitHubVariables.getVariableOrSecretValue(gitHubVariables, gitHubSecrets, githubVariableName);
                        if (gitHubSecretOrVariableValue) {
                            logger.info(SettingsFileProcessorMessages.substitutingVariable(fullyQualifiedVariableName));
                            json[key] = gitHubSecretOrVariableValue;
                        }
                    }
                }
            }
        }
    }

    private static isDeployRequiredKey(element: any, key: string, prefix: string): boolean {
        if (prefix !== "") {
            return false;
        }

        if (key.toUpperCase() !== SettingsFile.DeployProperty.toUpperCase()) {
            return false;
        }

        if (!element.hasOwnProperty(SettingsFile.RequiredProperty)) {
            return false;
        }


        const required = element[SettingsFile.RequiredProperty];
        if (!required) {
            return false;
        }

        return Array.isArray(required);
    }

    private static getDeployRequiredKey(element: any, key: string, prefix: string): any | undefined {
        if (prefix !== "") {
            return undefined;
        }

        if (key.toUpperCase() !== SettingsFile.DeployProperty.toUpperCase()) {
            return undefined;
        }

        if (!element.hasOwnProperty(SettingsFile.RequiredProperty)) {
            return undefined;
        }


        const required = element[SettingsFile.RequiredProperty];
        if (!required) {
            return undefined;
        }

        if (!Array.isArray(required)) {
            return undefined;
        }

        return element;
    }

    private static getDeployRequiredVariables(element: any): AppSettingRequiredVariable[] {

        const required = element[SettingsFile.RequiredProperty];
        if (required) {
            if (Array.isArray(required)) {
                let requiredVariables: AppSettingRequiredVariable[] = [];
                for (let index = 0; index < required.length; index++) {
                    const requiredSection = required[index];

                    if (requiredSection.hasOwnProperty(SettingsFile.KeysProperty)) {

                        if (requiredSection.hasOwnProperty(SettingsFile.DisabledProperty)) {
                            const disabled = requiredSection[SettingsFile.DisabledProperty];
                            if (disabled) {
                                continue;
                            }
                        }

                        const keys = requiredSection[SettingsFile.KeysProperty];
                        if (keys) {
                            requiredVariables.push(...keys.map((key: string) => new AppSettingRequiredVariable(key, GitHubVariables.calculateVariableOrSecretName(key))));
                        }
                    }
                }
                return requiredVariables;
            }
        }

        return [];
    }

    private static calculateFullyQualifiedVariableName(prefix: string, key: string): string {
        if (prefix === "") {
            return key;
        }
        return `${prefix}:${key}`;
    }

    async substitute(gitHubVariables: any, gitHubSecrets: any): Promise<boolean> {

        if (Object.keys(gitHubVariables).length === 0 && Object.keys(gitHubSecrets).length === 0) {
            return true;
        }

        try {
            const json = await this._readerWriter.readSettingsFile(this._path);
            AppSettingsJsonFileProcessor.substituteVariablesRecursively(this._logger, gitHubVariables, gitHubSecrets, json);
            await this._readerWriter.writeSettingsFile(this._path, json);
            this._logger.info(SettingsFileProcessorMessages.substitutingSucceeded(this._path));
            return true;
        } catch (error) {
            this._logger.error(SettingsFileProcessorMessages.unknownError(this._path, error));
            return false;
        }
    }

    async getVariables(path: string): Promise<AppSettingVariables> {

        const json = await this._readerWriter.readSettingsFile(path);
        const variables: string[] = [];
        const requiredVariables: AppSettingRequiredVariable[] = [];
        AppSettingsJsonFileProcessor.accumulateVariablesRecursively(this._logger, json, variables, requiredVariables);

        return Promise.resolve({variables, requiredVariables});
    }
}