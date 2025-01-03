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
        const projectName = github.context.repo.repo;
        logger.info(`Scanning settings files:'${filesParam}', in GitHub project ${projectName}'`);

        const gitHubSecrets = (secretsParam !== null && secretsParam !== undefined ? JSON.parse(secretsParam) : {}) ?? {};
        const gitHubEnvironmentVariables = (variablesParam !== null && variablesParam !== undefined ? JSON.parse(variablesParam) : {}) ?? {};

        const globParser = new GlobPatternParser();
        const jsonFileReader = new AppSettingsJsonFileReader();
        const configurationSets = await ConfigurationSets.create(logger, globParser, jsonFileReader, filesParam);
        if (configurationSets.hasNone) {
            logger.info('No settings files found, skipping variable substitution');
            return;
        } else {
            const verified = configurationSets.verifyConfiguration(gitHubEnvironmentVariables, gitHubSecrets);
            if (!verified) {
                return;
            }

            //TODO: Substitute: walk each configuration set, for each settings file:
            // 1. substitute the variables with the values from the variables/secrets (in-memory), then
            // 2. write those (in-memory) files to disk (in their original locations). 
        }
    } catch (error: unknown) {
        let message = "An unknown error occurred while processing the settings files";
        if (error instanceof Error) {
            message = error.message;
        }
        logger.error(message);
    }
}