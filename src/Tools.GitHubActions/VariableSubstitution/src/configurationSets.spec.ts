import {ConfigurationSets, ConfigurationSetsMessages} from "./configurationSets";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";
import {IAppSettingsJsonFileReaderWriter} from "./appSettingsJsonFileReaderWriter";

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
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            expect(sets.hasNone).toBe(true);
            expect(globParser.parseFiles).toHaveBeenCalledWith([]);
            expect(jsonFileReader.readAppSettingsFile).not.toHaveBeenCalled();
            expect(logger.warning).toHaveBeenCalledWith('No settings files found in this repository, applying the glob patterns: ');
        });

        it('should create a single set, when has one file at the root', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["afile.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(_path => Promise.resolve({
                    "aname": "avalue"
                })),
                writeAppSettingsFile: jest.fn(),
            };


            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual(".");
            expect(sets.sets[0].settingFiles.length).toEqual(1);
            expect(sets.sets[0].settingFiles[0].path).toEqual("afile.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname"]);
            expect(sets.sets[0].requiredVariables).toEqual([]);
            expect(globParser.parseFiles).toHaveBeenCalledWith([]);
            expect(jsonFileReader.readAppSettingsFile).toHaveBeenCalledWith("afile.json");
            expect(logger.info).toHaveBeenCalledWith('Found settings files, in these hosts:\n\t.:\n\t\tafile.json');
        });

        it('should create a single set, when has one file in a directory', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(_path => Promise.resolve({
                    "aname": "avalue"
                })),
                writeAppSettingsFile: jest.fn(),
            };

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual("apath");
            expect(sets.sets[0].settingFiles.length).toEqual(1);
            expect(sets.sets[0].settingFiles[0].path).toEqual("apath/afile.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname"]);
            expect(sets.sets[0].requiredVariables).toEqual([]);
            expect(globParser.parseFiles).toHaveBeenCalledWith([]);
            expect(jsonFileReader.readAppSettingsFile).toHaveBeenCalledWith("apath/afile.json");
            expect(logger.info).toHaveBeenCalledWith('Found settings files, in these hosts:\n\tapath:\n\t\tafile.json');
        });

        it('should create a single set, when has many files in same directory', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json", "apath/afile2.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };
            jsonFileReader.readAppSettingsFile
                .mockResolvedValueOnce({
                    "aname1": "avalue"
                })
                .mockResolvedValueOnce({
                    "aname2": "avalue"
                });

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual("apath");
            expect(sets.sets[0].settingFiles.length).toEqual(2);
            expect(sets.sets[0].settingFiles[0].path).toEqual("apath/afile1.json");
            expect(sets.sets[0].settingFiles[1].path).toEqual("apath/afile2.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname1", "aname2"]);
            expect(sets.sets[0].requiredVariables).toEqual([]);
            expect(globParser.parseFiles).toHaveBeenCalledWith([]);
            expect(jsonFileReader.readAppSettingsFile).toHaveBeenCalledWith("apath/afile1.json");
            expect(jsonFileReader.readAppSettingsFile).toHaveBeenCalledWith("apath/afile2.json");
            expect(logger.info).toHaveBeenCalledWith('Found settings files, in these hosts:\n\tapath:\n\t\tafile1.json,\n\t\tafile2.json');
        });

        it('should create a single set with combined required variables, when both files have Required settings', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json", "apath/afile2.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };
            jsonFileReader.readAppSettingsFile
                .mockResolvedValueOnce({
                    "aname1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": ["arequired1", "arequired2"]
                            }
                        ]
                    }
                })
                .mockResolvedValueOnce({
                    "aname2": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": ["arequired2", "arequired3"]
                            }
                        ]
                    }
                });

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            expect(sets.hasNone).toBe(false);
            expect(sets.length).toBe(1);
            expect(sets.sets[0].hostProjectPath).toEqual("apath");
            expect(sets.sets[0].settingFiles.length).toEqual(2);
            expect(sets.sets[0].settingFiles[0].path).toEqual("apath/afile1.json");
            expect(sets.sets[0].settingFiles[1].path).toEqual("apath/afile2.json");
            expect(sets.sets[0].definedVariables).toEqual(["aname1", "aname2"]);
            expect(sets.sets[0].requiredVariables).toEqual(["arequired1", "arequired2", "arequired3"]);
            expect(globParser.parseFiles).toHaveBeenCalledWith([]);
            expect(jsonFileReader.readAppSettingsFile).toHaveBeenCalledWith("apath/afile1.json");
            expect(jsonFileReader.readAppSettingsFile).toHaveBeenCalledWith("apath/afile2.json");
            expect(logger.info).toHaveBeenCalledWith('Found settings files, in these hosts:\n\tapath:\n\t\tafile1.json,\n\t\tafile2.json');
        });
    });

    describe('verifyConfiguration', () => {
        it('should return true, when there are no sets', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve([])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            const result = sets.verifyConfiguration({}, {});

            expect(result).toBe(true)
        });

        it('should return true, when the set contains no required', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };
            jsonFileReader.readAppSettingsFile
                .mockResolvedValueOnce({
                    "aname": "avalue"
                });

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            const result = sets.verifyConfiguration({}, {});

            expect(result).toBe(true);
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.verificationSucceeded());
        });

        it('should return false, when the set defines required variable, but no variable/secret exists in GitHub', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };
            jsonFileReader.readAppSettingsFile
                .mockResolvedValueOnce({
                    "arequired-arequired": {
                        "aname": "avalue"
                    },
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": ["arequired-arequired:aname"]
                            }
                        ]
                    }
                });

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            const result = sets.verifyConfiguration({}, {});

            expect(result).toBe(false);
            expect(logger.error).toHaveBeenCalledWith(ConfigurationSetsMessages.verificationFailed());
        });

        it('should return true, when the set defines required variable, and variable exists in GitHub', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };
            jsonFileReader.readAppSettingsFile
                .mockResolvedValueOnce({
                    "arequired-arequired": {
                        "aname": "avalue"
                    },
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": ["arequired-arequired:aname"]
                            }
                        ]
                    }
                });

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            const result = sets.verifyConfiguration({"AREQUIRED_AREQUIRED_ANAME": "avalue"}, {});

            expect(result).toBe(true);
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.verificationSucceeded());
        });

        it('should return true, when the set defines required variable, and secret exists in GitHub', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };
            jsonFileReader.readAppSettingsFile
                .mockResolvedValueOnce({
                    "arequired-arequired": {
                        "aname": "avalue"
                    },
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": ["arequired-arequired:aname"]
                            }
                        ]
                    }
                });

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            const result = sets.verifyConfiguration({}, {"AREQUIRED_AREQUIRED_ANAME": "avalue"});

            expect(result).toBe(true);
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.verificationSucceeded());
        });
    });

    describe('substituteVariables', () => {
        it('should return true, when there are no sets', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve([])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            const result = await sets.substituteVariables({}, {});

            expect(result).toBe(true)
        });

        it('should return true, when GitHub contains no variables or secrets', async () => {

            const globParser: jest.Mocked<IGlobPatternParser> = {
                parseFiles: jest.fn(_matches => Promise.resolve(["apath/afile1.json"])),
            };
            const jsonFileReader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
                readAppSettingsFile: jest.fn(),
                writeAppSettingsFile: jest.fn(),
            };
            jsonFileReader.readAppSettingsFile
                .mockResolvedValueOnce({
                    "aname": "avalue"
                });

            const sets = await ConfigurationSets.create(logger, globParser, jsonFileReader, '');

            const result = await sets.substituteVariables({}, {});

            expect(result).toBe(true);
            expect(logger.info).toHaveBeenCalledWith(ConfigurationSetsMessages.substitutionSucceeded());
        });
    });
});