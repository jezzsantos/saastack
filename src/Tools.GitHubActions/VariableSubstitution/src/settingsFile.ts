import {IAppSettingsJsonFileReaderWriter} from "./appSettingsJsonFileReaderWriter";
import {ILogger} from "./logger";

export interface ISettingsFile {
    readonly path: string;
    readonly variables: string[];
    readonly hasRequired: boolean;
    readonly requiredVariables: string[];

    substitute(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): Promise<boolean>;
}

export class SettingsFile implements ISettingsFile {

    public static DeployProperty: string = "Deploy";
    public static RequiredProperty: string = "Required";
    public static KeysProperty: string = "Keys";
    public static DisabledProperty: string = "Disabled";
    private readonly _reader: IAppSettingsJsonFileReaderWriter;

    private constructor(reader: IAppSettingsJsonFileReaderWriter, path: string, variables: string[], requiredVariables: string[]) {
        this._reader = reader;
        this._path = path;
        this._variables = variables;
        this._requiredVariables = requiredVariables;
    }

    _path: string;

    get path(): string {
        return this._path;
    }

    _variables: string[];

    get variables(): string[] {
        return this._variables;
    }

    _requiredVariables: string[];

    get requiredVariables(): string[] {
        return this._requiredVariables;
    }

    get hasRequired(): boolean {
        return this._requiredVariables.length > 0;
    }

    public static async create(reader: IAppSettingsJsonFileReaderWriter, path: string): Promise<SettingsFile> {

        const json = await reader.readAppSettingsFile(path);
        const variables: string[] = [];
        const requiredVariables: string[] = [];
        SettingsFile.accumulateVariablesRecursively(json, variables, requiredVariables);
        return new SettingsFile(reader, path, variables, requiredVariables);
    }

    public static isDefinedInGitHubVariables(gitHubVariables: any, gitHubSecrets: any, gitHubVariableName: string): boolean {

        return gitHubVariables.hasOwnProperty(gitHubVariableName) || gitHubSecrets.hasOwnProperty(gitHubVariableName);
    }

    public static getGitHubVariableOrSecretValue(gitHubVariables: any, gitHubSecrets: any, gitHubVariableName: string): any | undefined {

        if (gitHubVariables.hasOwnProperty(gitHubVariableName)) {
            return gitHubVariables[gitHubVariableName];
        }
        if (gitHubSecrets.hasOwnProperty(gitHubVariableName)) {
            return gitHubSecrets[gitHubVariableName];
        }

        return undefined;
    }

    public static calculateGitHubVariableName(fullyQualifiedVariableName: string) {
        // refer to: https://docs.github.com/en/actions/security-for-github-actions/security-guides/using-secrets-in-github-actions#naming-your-secrets
        return fullyQualifiedVariableName
            .toUpperCase()
            .replace(/[^A-Z0-9_]/g, '_');
    }

    private static accumulateVariablesRecursively(json: any, variables: string[], requiredVariables: string[], prefix: string = "") {
        for (const key in json) {
            if (json.hasOwnProperty(key)) {
                const element = json[key];
                const fullyQualifiedVariableName = SettingsFile.CalculateFullyQualifiedVariableName(prefix, key);
                if (typeof element === "object") {
                    if (SettingsFile.isDeployRequiredKey(element, key, prefix)) {
                        const required = SettingsFile.getDeployRequiredVariables(element);
                        if (required.length > 0) {
                            requiredVariables.push(...required);
                        }
                    } else {
                        SettingsFile.accumulateVariablesRecursively(element, variables, requiredVariables, fullyQualifiedVariableName);
                    }
                } else {
                    variables.push(fullyQualifiedVariableName);
                }
            }
        }
    }

    private static CalculateFullyQualifiedVariableName(prefix: string, key: string): string {
        if (prefix === "") {
            return key;
        }
        return `${prefix}:${key}`;
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

    private static getDeployRequiredVariables(element: any): string[] {

        const required = element[SettingsFile.RequiredProperty];
        if (required) {
            if (Array.isArray(required)) {
                let requiredVariables: string[] = [];
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
                            requiredVariables.push(...keys);
                        }
                    }
                }
                return requiredVariables;
            }
        }

        return [];
    }

    private static assignVariablesRecursively(logger: ILogger, gitHubVariables: any, gitHubSecrets: any, json: any, prefix: string = "") {
        for (const key in json) {
            if (json.hasOwnProperty(key)) {
                const element = json[key];
                const fullyQualifiedVariableName = SettingsFile.CalculateFullyQualifiedVariableName(prefix, key);
                if (typeof element === "object") {
                    if (!SettingsFile.isDeployRequiredKey(element, key, prefix)) {
                        SettingsFile.assignVariablesRecursively(logger, gitHubVariables, gitHubSecrets, element, fullyQualifiedVariableName);
                    }
                } else {
                    if (typeof element === "string" || typeof element === "number" || typeof element === "boolean") {
                        const githubVariableName = SettingsFile.calculateGitHubVariableName(fullyQualifiedVariableName);
                        const gitHubSecretOrVariableValue = SettingsFile.getGitHubVariableOrSecretValue(gitHubVariables, gitHubSecrets, githubVariableName);
                        if (gitHubSecretOrVariableValue) {
                            logger.info(SettingsFileMessages.substitutingVariable(fullyQualifiedVariableName));
                            json[key] = gitHubSecretOrVariableValue;
                        }
                    }
                }
            }
        }
    }

    async substitute(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): Promise<boolean> {
        logger.info(SettingsFileMessages.substitutingStarted(this.path));
        try {
            const json = await this._reader.readAppSettingsFile(this.path);
            SettingsFile.assignVariablesRecursively(logger, gitHubVariables, gitHubSecrets, json);
            await this._reader.writeAppSettingsFile(this.path, json);
            logger.info(SettingsFileMessages.substitutingSucceeded(this.path));
            return true;
        } catch (error) {
            logger.error(SettingsFileMessages.unknownError(this.path, error));
            return false;
        }
    }
}

export class SettingsFileMessages {
    public static readonly substitutingStarted = (path: string) => `\t\tSubstituting values into settings file '${path}'`;
    public static readonly substitutingSucceeded = (path: string) => `\t\tSubstituting values into settings file '${path}' -> Successful!`;
    public static readonly substitutingVariable = (fullyQualifiedVariableName: string) => `\t\t\tSubstituted '${fullyQualifiedVariableName}' with new value from GitHub environment variable or secret`;

    public static unknownError = (path: string, error: any): string => `\t\tUnexpected error '${error}' substituting GitHub environment variables or secrets into setting file: '${path}'`;
}