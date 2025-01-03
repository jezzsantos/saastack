import * as core from "@actions/core";
import * as github from "@actions/github";
import {ConfigurationSets} from "./configurationSets";
import {Logger} from "./logger";
import {GlobPatternParser} from "./globPatternParser";
import {AppSettingsJsonFileReader} from "./appSettingsJsonFileReader";

run().then();

async function run() {
    const logger = new Logger();

    try {

        const filesParam = core.getInput('files', {required: true});
        const secretsParam = core.getInput('secrets', {required: true});
        const variablesParam = core.getInput('variables', {required: true});
        const authorName = github.context.repo.owner;
        const projectName = github.context.repo.repo;
        logger.info(IndexErrors.writeInputs(filesParam, authorName, projectName));

        const gitHubSecrets = (secretsParam !== null && secretsParam !== undefined ? JSON.parse(secretsParam) : {}) ?? {};
        const gitHubEnvironmentVariables = (variablesParam !== null && variablesParam !== undefined ? JSON.parse(variablesParam) : {}) ?? {};

        const globParser = new GlobPatternParser();
        const jsonFileReader = new AppSettingsJsonFileReader();
        const configurationSets = await ConfigurationSets.create(logger, globParser, jsonFileReader, filesParam);
        if (configurationSets.hasNone) {
            logger.info(IndexErrors.abortNoSettings());
            return;
        } else {
            const verified = configurationSets.verifyConfiguration(gitHubEnvironmentVariables, gitHubSecrets);
            if (!verified) {
                return;
            }

            configurationSets.substituteVariables(gitHubEnvironmentVariables, gitHubSecrets);
        }
    } catch (error: unknown) {
        let message = IndexErrors.unknownError();
        if (error instanceof Error) {
            message = error.message;
        }
        logger.error(message);
    }
}

class IndexErrors {
    public static unknownError = () => "An unknown error occurred while processing the settings files";
    public static writeInputs = (filesParam: string, authorName: string, projectName: string) => `Scanning settings files: '${filesParam}', in GitHub project '${authorName}/${projectName}'`;
    public static abortNoSettings = () => 'No settings files found in this repository, skipping variable substitution altogether';
}