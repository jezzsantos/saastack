import {ISettingsFile, SettingsFile} from "./settingsFile";
import {ILogger} from "./logger";

export interface IConfigurationSet {
    readonly hostProjectPath: string;
    readonly settingFiles: ISettingsFile[];
    readonly requiredVariables: string[];
    readonly definedVariables: string[];

    accumulateVariables(): void;

    verify(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean;
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

    verify(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean {
        logger.info(`\tVerifying settings files in host: '${this.hostProjectPath}'`);
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
            let index = 0;
            const count = missingVariables.length;
            const presentation = missingVariables.map(pair => `${++index}. ${pair.gitHubVariableName} (alias: ${pair.requiredVariable})`).join(',\n\t\t');
            logger.error(`\tThe following '${count}' Required GitHub environment variables (or secrets) have not been defined in the environment variables (or secrets) of this GitHub project:\n\t\t${presentation}`);
        }
        if (redundantVariables.length > 0) {
            let index = 0;
            const count = redundantVariables.length;
            const presentation = redundantVariables.map(rv => `${++index}. ${rv}`).join(',\n\t\t');
            logger.warning(`\tThe following '${count}' Required variables are not yet defined in any of the settings files of this host! Consider either defining them in one of the settings files of this host, OR remove them from the '${SettingsFile.DeployProperty} -> ${SettingsFile.RequiredProperty} -> ${SettingsFile.KeysProperty}' section of the settings files in this host:\n\t\t${presentation}`);
        }
        if (confirmedVariables.length > 0) {
            let index = 0;
            const presentation = confirmedVariables.map(pair => `${++index}. ${pair.gitHubVariableName} (alias: ${pair.requiredVariable})`).join(',\n\t\t');
            const count = confirmedVariables.length;
            logger.info(`\tThe following '${count}' Required GitHub environment variables (or secrets) have been found in the environment variables (or secrets) of this GitHub project:\n\t\t${presentation}`);
        }

        if (!isSetVerified) {
            logger.error(`\tVerification settings files in host: '${this.hostProjectPath}' -> Failed! there is at least one missing required environment variable or secret in this GitHub project`);
        } else {
            logger.info(`\tVerifying settings files in host: '${this.hostProjectPath}' -> Successful!`);
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