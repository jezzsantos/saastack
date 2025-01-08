import {ConfigurationSets, ConfigurationSetsMessages} from "./configurationSets";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";
import {IAppSettingsReaderWriterFactory} from "./appSettingsReaderWriterFactory";
import {AppSettingRequiredVariable, ISettingsFileProcessor} from "./settingsFileProcessor";

describe('ConfigurationSets', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    describe('create', () => {

        it('should warn and be empty when constructed with no files', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve([])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn(),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            expect(sets.hasNone).toBe(true);
            expect(globParser.parseFiles).toHaveBeenCalledWith(["aglobpattern"]);
            expect(readerWriterFactory.createReadWriter).not.toHaveBeenCalled();
            expect(logger.warning).toHaveBeenCalledWith(ConfigurationSetsMessages.noSettingsFound('aglobpattern'));
        });

        it('should create a single set, when has one file at the root', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["afile.json"])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn(_path => Promise.resolve({variables: ["aname"], requiredVariables: []})),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual(".");
            expect(sets.sets[0].settingFiles.length).toEqual(1);
            expect(sets.sets[0].settingFiles[0].path).toEqual("afile.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname"]);
            expect(sets.sets[0].requiredVariables).toEqual([]);
            expect(globParser.parseFiles).toHaveBeenCalledWith(["aglobpattern"]);
            expect(readerWriterFactory.createReadWriter).toHaveBeenCalledWith(logger, "afile.json");
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.foundSettingsFiles(sets.sets));
        });

        it('should create a single set, when has one file in a directory', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile.json"])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn((_path)=> Promise.resolve({variables: ["aname"], requiredVariables: []})),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual("apath");
            expect(sets.sets[0].settingFiles.length).toEqual(1);
            expect(sets.sets[0].settingFiles[0].path).toEqual("apath/afile.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname"]);
            expect(sets.sets[0].requiredVariables).toEqual([]);
            expect(globParser.parseFiles).toHaveBeenCalledWith(["aglobpattern"]);
            expect(readerWriterFactory.createReadWriter).toHaveBeenCalledWith(logger, "apath/afile.json");
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.foundSettingsFiles(sets.sets));
        });

        it('should create a single set, when has many files in same directory', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json", "apath/afile2.json"])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn((_path)=> Promise.resolve({variables: ["aname1", "aname2"], requiredVariables: []})),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual("apath");
            expect(sets.sets[0].settingFiles.length).toEqual(2);
            expect(sets.sets[0].settingFiles[0].path).toEqual("apath/afile1.json");
            expect(sets.sets[0].settingFiles[1].path).toEqual("apath/afile2.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname1", "aname2"]);
            expect(sets.sets[0].requiredVariables).toEqual([]);
            expect(globParser.parseFiles).toHaveBeenCalledWith(["aglobpattern"]);
            expect(readerWriterFactory.createReadWriter).toHaveBeenCalledWith(logger, "apath/afile1.json");
            expect(readerWriterFactory.createReadWriter).toHaveBeenCalledWith(logger, "apath/afile2.json");
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.foundSettingsFiles(sets.sets));
        });

        it('should create a single set with combined required variables, when both files have Required settings', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json", "apath/afile2.json"])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn(),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };
            readerWriter.getVariables
                .mockResolvedValueOnce({variables: ["aname1"], requiredVariables: [new AppSettingRequiredVariable("arequired1", "AREQUIRED1"), new AppSettingRequiredVariable("arequired2", "AREQUIRED2")]})
                .mockResolvedValueOnce({variables: ["aname2"], requiredVariables: [new AppSettingRequiredVariable("arequired2", "AREQUIRED2"), new AppSettingRequiredVariable("arequired3", "AREQUIRED3")]});

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual("apath");
            expect(sets.sets[0].settingFiles.length).toEqual(2);
            expect(sets.sets[0].settingFiles[0].path).toEqual("apath/afile1.json");
            expect(sets.sets[0].settingFiles[1].path).toEqual("apath/afile2.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname1", "aname2"]);
            expect(sets.sets[0].requiredVariables).toEqual([new AppSettingRequiredVariable("arequired1", "AREQUIRED1"), new AppSettingRequiredVariable("arequired2", "AREQUIRED2"), new AppSettingRequiredVariable("arequired3", "AREQUIRED3")]);
            expect(globParser.parseFiles).toHaveBeenCalledWith(["aglobpattern"]);
            expect(readerWriterFactory.createReadWriter).toHaveBeenCalledWith(logger, "apath/afile1.json");
            expect(readerWriterFactory.createReadWriter).toHaveBeenCalledWith(logger, "apath/afile2.json");
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.foundSettingsFiles(sets.sets));
        });
    });

    describe('verifyConfiguration', () => {
        it('should return true, when there are no sets', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve([])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn(),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            const result = sets.verifyConfiguration({}, {});

            expect(result).toBe(true)
            expect(logger.info).not.toHaveBeenCalledWith(ConfigurationSetsMessages.startVerification());
        });

        it('should return true, when the set contains no required', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn((_path)=> Promise.resolve({variables: ["aname"], requiredVariables: []})),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            const result = sets.verifyConfiguration({}, {});

            expect(result).toBe(true);
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.startVerification());
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.verificationSucceeded());
        });

        it('should return false, when the set defines required variable, but no variable/secret exists in GitHub', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn((_path)=> Promise.resolve({variables: ["aname"], requiredVariables: [new AppSettingRequiredVariable("aname", "ANAME")]})),
                substitute: jest.fn(),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            const result = sets.verifyConfiguration({}, {});

            expect(result).toBe(false);
            expect(logger.error).toHaveBeenCalledWith(ConfigurationSetsMessages.verificationFailed());
        });
    });

    describe('substituteVariables', () => {
        it('should return true, when there are no sets', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve([])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn(),
                substitute: jest.fn().mockReturnValue(true),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            const result = await sets.substituteVariables({"avariable":"avalue1"}, {"asecret":"avalue2"});

            expect(result).toBe(true)
            expect(readerWriter.substitute).not.toHaveBeenCalled();
            expect(logger.info).not.toHaveBeenCalledWith(ConfigurationSetsMessages.startSubstitution());
        });

        it('should return true, when GitHub contains no variables or secrets', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const readerWriter: jest.Mocked<ISettingsFileProcessor> = {
                getVariables: jest.fn().mockReturnValue({variables: ["aname"], requiredVariables: []}),
                substitute: jest.fn().mockReturnValue(true),
            };
            const readerWriterFactory: jest.Mocked<IAppSettingsReaderWriterFactory> = {
                createReadWriter: jest.fn((_logger, _filePath) => Promise.resolve(readerWriter))
            };

            const sets = await ConfigurationSets.create(logger, globParser, readerWriterFactory, 'aglobpattern');

            const result = await sets.substituteVariables({}, {});

            expect(result).toBe(true);
            expect(readerWriter.substitute).toHaveBeenCalledWith({}, {});
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.startSubstitution());
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.substitutionSucceeded());
        });
    });
});