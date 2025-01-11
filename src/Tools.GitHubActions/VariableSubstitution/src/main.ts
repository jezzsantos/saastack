import {GlobPatternParser} from "./globPatternParser";
import {ConfigurationSets} from "./configurationSets";
import {Logger} from "./logger";
import {IGitHubAction} from "./githubAction";
import {IAppSettingsReaderWriterFactory} from "./appSettingsReaderWriterFactory";


export class Main {
    private readonly _logger: Logger;
    private readonly _globParser: GlobPatternParser;
    private readonly _readerWriterFactory: IAppSettingsReaderWriterFactory;
    private readonly _action: IGitHubAction;

    constructor(logger: Logger, action: IGitHubAction, globParser: GlobPatternParser, readerWriterFactory: IAppSettingsReaderWriterFactory) {
        this._logger = logger;
        this._action = action;
        this._globParser = globParser;
        this._readerWriterFactory = readerWriterFactory;
    }

    public async run() {
        try {
            const filesParam = this._action.getRequiredWorkflowInput('files');
            const authorName = this._action.getRepositoryOwner();
            const projectName = this._action.getRepositoryName();
            const gitHubSecrets = this._action.getGitHubSecrets();
            const gitHubEnvironmentVariables = this._action.getGitHubVariables();
            const warnOnAdditionalVars: boolean = this._action.getOptionalWorkflowInput('warnOnAdditionalVars', "false") === "true";
            const ignoreAdditionalVars: string = this._action.getOptionalWorkflowInput('ignoreAdditionalVars', "");
            const warnOnDuplicateVars: boolean = true;
            const warningOptions = new WarningOptions(warnOnAdditionalVars, ignoreAdditionalVars, warnOnDuplicateVars);
            this._logger.info(MainMessages.writeInputs(filesParam, authorName, projectName, warningOptions));

            const configurationSets = await ConfigurationSets.create(this._logger, this._globParser, this._readerWriterFactory, filesParam, warningOptions);
            if (configurationSets.hasNone) {
                this._logger.info(MainMessages.abortNoSettings());
                return;
            } else {
                const verified = configurationSets.verifyConfiguration(gitHubEnvironmentVariables, gitHubSecrets);
                if (!verified) {
                    return;
                }

                await configurationSets.substituteVariables(gitHubEnvironmentVariables, gitHubSecrets);
            }
        } catch (error: unknown) {
            let message = MainMessages.unknownError();
            if (error instanceof Error) {
                message = error.message;
            }
            this._logger.error(message);
        }
    }
}

export class WarningOptions {
    public warnOnAdditionalVariables: boolean = false;
    public ignoreAdditionalVariableExpression: string = "";
    public warnOnDuplicateVariables: boolean = false;

    constructor(warnOnAdditionalVariables?: boolean, ignoreAdditionalVariableExpression?: string, warnOnDuplicateVariables?: boolean) {
        this.warnOnAdditionalVariables = warnOnAdditionalVariables ?? false;
        this.ignoreAdditionalVariableExpression = ignoreAdditionalVariableExpression ?? "";
        this.warnOnDuplicateVariables = warnOnDuplicateVariables ?? false;
    }
}

export class MainMessages {
    public static unknownError = () => "An unknown error occurred while processing the settings files";
    public static writeInputs = (filesParam: string, authorName: string, projectName: string, warningOptions: WarningOptions) => `Scanning settings files: '${filesParam}', in GitHub project '${authorName}/${projectName}', with options ${JSON.stringify(warningOptions)}`;
    public static abortNoSettings = () => 'No settings files found in this repository, skipping variable substitution altogether';
}