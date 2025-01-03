import {ISettingsFile, SettingsFile} from "./settingsFile";
import {ILogger} from "./logger";

export interface IConfigurationSet {
    readonly hostProjectPath: string;
    readonly settingFiles: ISettingsFile[];
    readonly requiredVariables: string[];
    readonly definedVariables: string[];

    accumulateVariables(): void;

    verify(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean;

    substitute(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean;
}

export class ConfigurationSet implements IConfigurationSet {
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

    substitute(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean {
        //TODO: Substitute: walk each configuration set, for each settings file:
        // 1. substitute the variables with the values from the variables/secrets (in-memory), then
        // 2. write those (in-memory) files to disk (in their original locations). 

        return true;
    }

    verify(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean {
        logger.info(ConfigurationSetErrors.startVerification(this.hostProjectPath));
        let isSetVerified = true;
        const confirmedVariables: Record<string, string>[] = [];
        const missingVariables: Record<string, string>[] = [];
        const redundantVariables: string[] = [];
        for (const requiredVariable of this.requiredVariables) {
            if (!this.definedVariables.includes(requiredVariable)) {
                redundantVariables.push(requiredVariable);
                continue;
            }

            const gitHubVariableName = this.calculateGitHubVariableName(requiredVariable);
            if (!this.isDefinedInGitHubVariables(gitHubVariables, gitHubSecrets, gitHubVariableName)) {
                missingVariables.push({requiredVariable, gitHubVariableName});
                isSetVerified = false;
            } else {
                confirmedVariables.push({requiredVariable, gitHubVariableName});
            }
        }

        if (missingVariables.length > 0) {
            logger.error(ConfigurationSetErrors.missingRequiredVariables(missingVariables));
        }
        if (redundantVariables.length > 0) {
            logger.warning(ConfigurationSetErrors.redundantVariables(redundantVariables));
        }
        if (confirmedVariables.length > 0) {
            logger.info(ConfigurationSetErrors.foundConfirmedVariables(confirmedVariables));
        }

        if (!isSetVerified) {
            logger.error(ConfigurationSetErrors.verificationFailed(this.hostProjectPath));
        } else {
            logger.info(ConfigurationSetErrors.verificationSucceeded(this.hostProjectPath));
        }

        return isSetVerified;
    }

    accumulateVariables() {

        const files = this.settingFiles;
        for (const file of files) {
            this.accumulateDefinedVariables(file.variables);
            if (file.hasRequired) {
                this.accumulateRequiredVariables(file.requiredVariables);
            }
        }

    }

    private accumulateRequiredVariables(variables: string[]) {
        for (const variable of variables) {
            if (!this._requiredVariables.includes(variable)) {
                this._requiredVariables.push(variable);
            }
        }
    }

    private accumulateDefinedVariables(variables: string[]) {
        for (const variable of variables) {
            if (!this._definedVariables.includes(variable)) {
                this._definedVariables.push(variable);
            }
        }
    }

    private isDefinedInGitHubVariables(gitHubVariables: any, gitHubSecrets: any, name: string): boolean {

        return gitHubVariables.hasOwnProperty(name) || gitHubSecrets.hasOwnProperty(name);
    }

    private calculateGitHubVariableName(requiredVariable: string) {
        // refer to: https://docs.github.com/en/actions/security-for-github-actions/security-guides/using-secrets-in-github-actions#naming-your-secrets
        return requiredVariable
            .toUpperCase()
            .replace(/[^A-Z0-9_]/g, '_');
    }
}

export class ConfigurationSetErrors {
    public static startVerification = (hostProjectPath: string): string => `\tVerifying settings files in host: '${hostProjectPath}'`;
    public static missingRequiredVariables = (missingVariables: Record<string, string>[]): string => {
        let index = 0;
        const count = missingVariables.length;
        const presentation = missingVariables.map(pair => `${++index}. ${pair.gitHubVariableName} (alias: ${pair.requiredVariable})`).join(',\n\t\t');
        return `\tThe following '${count}' required GitHub environment variables (or secrets) of this host have not been defined in the environment variables (or secrets) of this GitHub project:\n\t\t${presentation}`;
    };
    public static redundantVariables = (redundantVariables: string[]): string => {
        let index = 0;
        const count = redundantVariables.length;
        const presentation = redundantVariables.map(rv => `${++index}. ${rv}`).join(',\n\t\t');
        return `\tThe following '${count}' required variables of this host are not yet defined in any of the settings files of this host! Consider either defining them in one of the settings files of this host, OR remove them from the '${SettingsFile.DeployProperty} -> ${SettingsFile.RequiredProperty} -> ${SettingsFile.KeysProperty}' section of the settings files in this host:\n\t\t${presentation}`;
    };
    public static foundConfirmedVariables = (confirmedVariables: Record<string, string>[]): string => {
        let index = 0;
        const presentation = confirmedVariables.map(pair => `${++index}. ${pair.gitHubVariableName} (alias: ${pair.requiredVariable})`).join(',\n\t\t');
        const count = confirmedVariables.length;
        return `\tThe following '${count}' required GitHub environment variables (or secrets) of this host were found in this GitHub project:\n\t\t${presentation}`;
    };
    public static verificationFailed = (hostProjectPath: string) => `\tVerification settings files in host: '${hostProjectPath}' -> Failed! there is at least one undefined environment variable or secret in this GitHub project`;
    public static verificationSucceeded = (hostProjectPath: string) => `\tVerifying settings files in host: '${hostProjectPath}' -> Successful!`
}