import {IAppSettingsReaderWriterFactory} from "./appSettingsReaderWriterFactory";
import {ILogger} from "./logger";
import {AppSettingRequiredVariable, AppSettingVariables, ISettingsFileProcessor} from "./settingsFileProcessor";
import {WarningOptions} from "./main";

export interface ISettingsFile {
    readonly path: string;
    readonly variables: string[];
    readonly hasRequired: boolean;
    readonly requiredVariables: AppSettingRequiredVariable[];

    substitute(logger: ILogger, warningOptions: WarningOptions, gitHubVariables: any, gitHubSecrets: any): Promise<boolean>;
}

export class SettingsFile implements ISettingsFile {

    public static DeployProperty: string = "Deploy";
    public static RequiredProperty: string = "Required";
    public static KeysProperty: string = "Keys";
    public static DisabledProperty: string = "Disabled";
    private readonly _readerWriter: ISettingsFileProcessor;

    private constructor(readerWriterFactory: ISettingsFileProcessor, path: string, variables: AppSettingVariables) {
        this._readerWriter = readerWriterFactory;
        this._path = path;
        this._variables = variables;
    }

    _path: string;

    get path(): string {
        return this._path;
    }

    _variables: AppSettingVariables;

    get variables(): string[] {
        return this._variables.variables;
    }

    get requiredVariables(): AppSettingRequiredVariable[] {
        return this._variables.requiredVariables;
    }

    get hasRequired(): boolean {
        return this._variables.requiredVariables.length > 0;
    }

    public static async create(logger: ILogger, readerWriterFactory: IAppSettingsReaderWriterFactory, path: string): Promise<SettingsFile> {

        const readerWriter = await readerWriterFactory.createReadWriter(logger, path);
        const variables = await readerWriter.getVariables(path);
        return new SettingsFile(readerWriter, path, variables);
    }

    async substitute(logger: ILogger, warningOptions: WarningOptions, gitHubVariables: any, gitHubSecrets: any): Promise<boolean> {
        logger.info(SettingsFileMessages.substitutingStarted(this.path));

        return await this._readerWriter.substitute(warningOptions, gitHubVariables, gitHubSecrets);
    }
}

export class SettingsFileMessages {
    public static readonly substitutingStarted = (path: string) => `\t\tSubstituting values into settings file '${path}'`;
}