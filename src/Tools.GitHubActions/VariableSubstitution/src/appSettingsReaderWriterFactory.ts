import {ILogger} from "./logger";
import {ISettingsFileProcessor} from "./settingsFileProcessor";
import {AppSettingsJsonFileReaderWriter, FlatFileReaderWriter} from "./fileReaderWriter";
import * as path from "node:path";
import {AppSettingsJsonFileProcessor} from "./appSettingsJsonFileProcessor";
import {FlatFileProcessor} from "./flatFileProcessor";

export interface IAppSettingsReaderWriterFactory {

    createReadWriter(logger: ILogger, filePath: string): Promise<ISettingsFileProcessor>;
}

export class AppSettingsReaderWriterFactory implements IAppSettingsReaderWriterFactory {
    createReadWriter(logger: ILogger, filePath: string): Promise<ISettingsFileProcessor> {
        const extension = path.extname(filePath);
        if (extension === ".json") {
            return Promise.resolve<ISettingsFileProcessor>(new AppSettingsJsonFileProcessor(logger, new AppSettingsJsonFileReaderWriter(), filePath));
        } else {
            return Promise.resolve<ISettingsFileProcessor>(new FlatFileProcessor(logger, new FlatFileReaderWriter(), filePath));
        }
    }
}