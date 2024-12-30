import core from "@actions/core";
import {glob} from "glob";

try {
    const filesParam = core.getInput('files', {required: true});
    const environmentParam = core.getInput('environment');
    core.info(`Scanning settings files:'${filesParam}', against GitHub project Environment: '${environmentParam}'`);

    const globs = filesParam.split(',');

    // Find the files from these globs
    const files = await glob(globs, {ignore: ['node_modules/**', 'bin/**', 'obj/**']});
    if (files.length === 0) {
        core.warning(`No settings files found in this repository, using the glob patterns: ${filesParam}`);
        core.info('Skipping variable substitution');
    } else {
        core.info(`Found settings files:\n\t${files.join(', \n\t')}`);

        // Get the JSON webhook payload for the event that triggered the workflow
        // const payload = JSON.stringify(github.context.payload, undefined, 2);
        // core.info(`The event payload: ${payload}`);
    }
} catch (error) {
    core.setFailed(error.message);
}