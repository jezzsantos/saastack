import {GlobPatternParser} from "./globPatternParser";
import {AppSettingsJsonFileReaderWriter} from "./appSettingsJsonFileReaderWriter";
import {ConfigurationSets} from "./configurationSets";
import {Logger} from "./logger";
import {IGitHubAction} from "./githubAction";


export class Main {
    private readonly _logger: Logger;
    private readonly _globParser: GlobPatternParser;
    private readonly _jsonFileReader: AppSettingsJsonFileReaderWriter;
    private readonly _action: IGitHubAction;

    constructor(logger: Logger, action: IGitHubAction, globParser: GlobPatternParser, jsonFileReader: AppSettingsJsonFileReaderWriter) {
        this._logger = logger;
        this._action = action;
        this._globParser = globParser;
        this._jsonFileReader = jsonFileReader;
    }

    public async run() {
        try {
            const filesParam = this._action.getRequiredWorkflowInput('files');
            const authorName = this._action.getRepositoryOwner();
            const projectName = this._action.getRepositoryName();
            const gitHubSecrets = this._action.getGitHubSecrets();
            const gitHubEnvironmentVariables = this._action.getGitHubVariables();
            this._logger.info(MainMessages.writeInputs(filesParam, authorName, projectName));

            const configurationSets = await ConfigurationSets.create(this._logger, this._globParser, this._jsonFileReader, filesParam);
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

export class MainMessages {
    public static unknownError = () => "An unknown error occurred while processing the settings files";
    public static writeInputs = (filesParam: string, authorName: string, projectName: string) => `Scanning settings files: '${filesParam}', in GitHub project '${authorName}/${projectName}'`;
    public static abortNoSettings = () => 'No settings files found in this repository, skipping variable substitution altogether';
}