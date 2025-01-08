import {Logger} from "./logger";
import {GlobPatternParser} from "./globPatternParser";
import {AppSettingsReaderWriterFactory} from "./appSettingsReaderWriterFactory";
import {Main} from "./main";
import {GithubAction} from "./githubAction";

run().then();

async function run() {
    const main = new Main(new Logger(), new GithubAction(), new GlobPatternParser(), new AppSettingsReaderWriterFactory());
    await main.run();
}

