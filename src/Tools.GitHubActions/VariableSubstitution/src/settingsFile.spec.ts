import {SettingsFile, SettingsFileMessages} from "./settingsFile";
import {IAppSettingsReaderWriterFactory} from "./appSettingsReaderWriterFactory";
import {ILogger} from "./logger";
import {ISettingsFileProcessor} from "./settingsFileProcessor";
import {WarningOptions} from "./main";

describe('create', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should return settings file', async () => {

        const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
            getVariables: jest.fn().mockReturnValue({variables: ["aname"], requiredVariables: ["aname"]}),
            substitute: jest.fn(),
        };
        const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
            createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
        };

        const settingsFile = await SettingsFile.create(logger, readerWriterFactory, "apath");

        expect(settingsFile.path).toEqual("apath");
        expect(settingsFile.variables).toEqual(["aname"]);
        expect(settingsFile.requiredVariables).toEqual(["aname"]);
        expect(settingsFile.hasRequired).toEqual(true);
        expect(readerWriterFactory.createReadWriter).toHaveBeenCalledWith(logger, "apath");
        expect(readerWriter.getVariables).toHaveBeenCalledWith("apath");
    });
});

describe('substitute', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should return reader writer substitution result', async () => {

        const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
            getVariables: jest.fn().mockReturnValue({variables: ["aname"], requiredVariables: ["aname"]}),
            substitute: jest.fn().mockReturnValue(true),
        };
        const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
            createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
        };

        const settingsFile = await SettingsFile.create(logger, readerWriterFactory, "apath");

        const result = await settingsFile.substitute(logger, new WarningOptions(), {"avariable": "avalue1"}, {"asecret": "avalue2"});

        expect(result).toEqual(true);
        expect(readerWriter.substitute).toHaveBeenCalledWith(new WarningOptions(), {"avariable": "avalue1"}, {"asecret": "avalue2"});
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingStarted("apath"));
    });
});
    
    