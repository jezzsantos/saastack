import {Logger} from "./logger";
import {GlobPatternParser} from "./globPatternParser";
import {AppSettingsJsonFileReaderWriter} from "./appSettingsJsonFileReaderWriter";
import {Main} from "./main";
import {GithubAction} from "./githubAction";

run().then();

async function run() {
    const main = new Main(new Logger(), new GithubAction(), new GlobPatternParser(), new AppSettingsJsonFileReaderWriter());
    await main.run();
}
