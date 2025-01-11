import {ISettingsFile, SettingsFile} from "./settingsFile";
import {ILogger} from "./logger";
import {AppSettingRequiredVariable, GitHubVariables} from "./settingsFileProcessor";
import {WarningOptions} from "./main";

export interface IConfigurationSet {
    readonly hostProjectPath: string;
    readonly settingFiles: ISettingsFile[];
    readonly requiredVariables: AppSettingRequiredVariable[];
    readonly definedVariables: string[];

    accumulateVariables(): void;

    verify(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean;

    substitute(logger: ILogger, warningOptions: WarningOptions, gitHubVariables: any, gitHubSecrets: any): Promise<boolean>;
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

    _requiredVariables: AppSettingRequiredVariable[];

    get requiredVariables(): AppSettingRequiredVariable[] {
        return this._requiredVariables;
    }

    _definedVariables: string[];

    get definedVariables(): string[] {
        return this._definedVariables;
    }

    async substitute(logger: ILogger, warningOptions: WarningOptions, gitHubVariables: any, gitHubSecrets: any): Promise<boolean> {
        logger.info(ConfigurationSetMessages.startSubstitution(this.hostProjectPath));
        let isSetSubstituted = true;
        for (const file of this.settingFiles) {
            const isSubstituted = await file.substitute(logger, warningOptions, gitHubVariables, gitHubSecrets);
            if (!isSubstituted) {
                isSetSubstituted = false;
            }
        }

        if (!isSetSubstituted) {
            logger.error(ConfigurationSetMessages.substitutionFailed(this.hostProjectPath));
        } else {
            logger.info(ConfigurationSetMessages.substitutionSucceeded(this.hostProjectPath));
        }

        return isSetSubstituted;
    }

    verify(logger: ILogger, gitHubVariables: any, gitHubSecrets: any): boolean {
        logger.info(ConfigurationSetMessages.startVerification(this.hostProjectPath));
        let isSetVerified = true;
        const confirmedVariables: Record<string, string>[] = [];
        const missingVariables: Record<string, string>[] = [];
        const redundantVariables: string[] = [];
        for (const requiredVariable of this.requiredVariables) {
            if (!this.definedVariables.includes(requiredVariable.name)) {
                redundantVariables.push(requiredVariable.name);
                continue;
            }

            if (!GitHubVariables.isDefined(gitHubVariables, gitHubSecrets, requiredVariable.gitHubVariableOrSecretName)) {
                missingVariables.push({
                    requiredVariable: requiredVariable.name,
                    gitHubVariableName: requiredVariable.gitHubVariableOrSecretName
                });
                isSetVerified = false;
            } else {
                confirmedVariables.push({
                    requiredVariable: requiredVariable.name,
                    gitHubVariableName: requiredVariable.gitHubVariableOrSecretName
                });
            }
        }

        if (missingVariables.length > 0) {
            logger.error(ConfigurationSetMessages.missingRequiredVariables(missingVariables));
        }
        if (redundantVariables.length > 0) {
            logger.warning(ConfigurationSetMessages.redundantVariables(redundantVariables));
        }
        if (confirmedVariables.length > 0) {
            logger.info(ConfigurationSetMessages.foundConfirmedVariables(confirmedVariables));
        }

        if (!isSetVerified) {
            logger.error(ConfigurationSetMessages.verificationFailed(this.hostProjectPath));
        } else {
            logger.info(ConfigurationSetMessages.verificationSucceeded(this.hostProjectPath));
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

    private accumulateRequiredVariables(variables: AppSettingRequiredVariable[]) {
        for (const variable of variables) {
            if (!this._requiredVariables.find(reqVar => reqVar.name === variable.name)) {
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
}

export class ConfigurationSetMessages {
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
    public static verificationFailed = (hostProjectPath: string) => `\tVerification of settings files in host: '${hostProjectPath}' -> Failed! there is at least one undefined environment variable or secret in this GitHub project`;
    public static verificationSucceeded = (hostProjectPath: string) => `\tVerifying settings files in host: '${hostProjectPath}' -> Successful!`;
    public static startSubstitution = (hostProjectPath: string): string => `\tSubstituting values in all settings files in host: '${hostProjectPath}'`;
    public static substitutionFailed = (hostProjectPath: string) => `\tSubstitution of values in all settings files in host: '${hostProjectPath}' -> Failed! there is was at least one error during the substitution process`;
    public static substitutionSucceeded = (hostProjectPath: string) => `\tSubstituting values in all settings files in host: '${hostProjectPath}' -> Successful!`


}