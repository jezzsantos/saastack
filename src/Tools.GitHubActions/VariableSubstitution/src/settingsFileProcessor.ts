export interface ISettingsFileProcessor {
    substitute(gitHubVariables: any, gitHubSecrets: any): Promise<boolean>;

    getVariables(path: string): Promise<AppSettingVariables>;
}

export class AppSettingVariables {
    public variables: string[] = [];
    public requiredVariables: AppSettingRequiredVariable[] = [];
}

export class AppSettingRequiredVariable {
    public name: string;
    public gitHubVariableOrSecretName: string;

    constructor(name: string, gitHubVariableOrSecretName: string) {
        this.name = name;
        this.gitHubVariableOrSecretName = gitHubVariableOrSecretName;
    }
}

export class GitHubVariables {

    public static isDefined(gitHubVariables: any, gitHubSecrets: any, gitHubVariableName: string): boolean {

        return gitHubVariables.hasOwnProperty(gitHubVariableName) || gitHubSecrets.hasOwnProperty(gitHubVariableName);
    }

    public static getVariableOrSecretValue(gitHubVariables: any, gitHubSecrets: any, gitHubVariableName: string): any | undefined {

        if (gitHubVariables.hasOwnProperty(gitHubVariableName)) {
            return gitHubVariables[gitHubVariableName];
        }
        if (gitHubSecrets.hasOwnProperty(gitHubVariableName)) {
            return gitHubSecrets[gitHubVariableName];
        }

        return undefined;
    }

    public static calculateVariableOrSecretName(fullyQualifiedVariableName: string) {
        // refer to: https://docs.github.com/en/actions/security-for-github-actions/security-guides/using-secrets-in-github-actions#naming-your-secrets
        return fullyQualifiedVariableName
            .toUpperCase()
            .replace(/[^A-Z0-9_]/g, '_');
    }
}


export class SettingsFileProcessorMessages {
    public static readonly substitutingSucceeded = (path: string) => `\t\tSubstituting values into settings file '${path}' -> Successful!`;
    public static readonly substitutingVariable = (fullyQualifiedVariableName: string) => `\t\t\tSubstituted '${fullyQualifiedVariableName}' with new value from GitHub environment variable or secret`;
    public static unknownError = (path: string, error: any): string => `\t\tUnexpected error '${error}' substituting GitHub environment variables or secrets into setting file: '${path}'`;
    public static redactedDeployMessage = () => {
        const now = new Date().toISOString();
        return `All keys substituted, and removed: '${now}'`;
    };
}