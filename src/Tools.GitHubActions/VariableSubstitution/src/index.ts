import * as core from "@actions/core";
import * as github from "@actions/github";
import {ConfigurationSets} from "./configurationSets";
import {Logger} from "./logger";
import {GlobPatternParser} from "./globPatternParser";

run().then();

async function run() {
    const logger = new Logger();

    try {

        const filesParam = core.getInput('files', {required: true});
        const secretsParam = core.getInput('secrets', {required: true});
        const variablesParam = core.getInput('variables', {required: true});
        const projectName = github.context.repo.repo;
        logger.info(`Scanning settings files:'${filesParam}', in GitHub project ${projectName}'`);

        const secrets = JSON.parse(secretsParam);
        const variables = JSON.parse(variablesParam);

        const globParser = new GlobPatternParser();
        const configurationSets = await ConfigurationSets.create(logger, globParser, filesParam);
        if (configurationSets.hasNone) {
            logger.info('Skipping variable substitution');
        } else {
            // Get the JSON webhook payload for the event that triggered the workflow
            // const payload = JSON.stringify(github.context.payload, undefined, 2);
            // core.info(`The event payload: ${payload}`);
        }
    } catch (error: unknown) {
        let message = "An unknown error occurred while processing the settings files";
        if (error instanceof Error) {
            message = error.message;
        }
        logger.error(message);
    }
}