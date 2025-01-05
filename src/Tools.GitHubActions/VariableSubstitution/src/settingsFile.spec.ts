import {SettingsFile, SettingsFileMessages} from "./settingsFile";
import {IAppSettingsJsonFileReaderWriter} from "./appSettingsJsonFileReaderWriter";
import {ILogger} from "./logger";

describe('create', () => {
    it('should return file, when file has no variables', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue({}),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(0);
    });

    it('should return file, when file has multi-level variables', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1.1": {
                        "Level2.1": "avalue1",
                        "Level2.2": {
                            "Level3.1": "avalue2"
                        }
                    },
                    "Level1.2": "avalue4"
                }),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(3);
        expect(file.variables[0]).toEqual("Level1.1:Level2.1");
        expect(file.variables[1]).toEqual("Level1.1:Level2.2:Level3.1");
        expect(file.variables[2]).toEqual("Level1.2");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file without Required, when file has incorrectly typed DeployRequired value', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": "adeploy"
                }),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(2);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.variables[1]).toEqual("Deploy");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file without Required, when file has incorrectly nested DeployRequired value', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": {
                        "Deploy": {
                            "Required": [
                                {
                                    "Keys": [
                                        "arequired1",
                                        "arequired2",
                                        "arequired3"]
                                }
                            ]
                        }
                    }
                }),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(3);
        expect(file.variables[0]).toEqual("Level1:Deploy:Required:0:Keys:0");
        expect(file.variables[1]).toEqual("Level1:Deploy:Required:0:Keys:1");
        expect(file.variables[2]).toEqual("Level1:Deploy:Required:0:Keys:2");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file without Required, when file has correct DeployRequired values, but explicitly disabled Keys', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Disabled": true,
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            }
                        ]
                    }
                }),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(1);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.hasRequired).toEqual(false);
        expect(file.requiredVariables.length).toEqual(0);
    });

    it('should return file with Required, when file has correct DeployRequired values, and not explicitly disabled Keys', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            }
                        ]
                    }
                }),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(1);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.hasRequired).toEqual(true);
        expect(file.requiredVariables.length).toEqual(3);
        expect(file.requiredVariables[0]).toEqual("arequired1");
        expect(file.requiredVariables[1]).toEqual("arequired2");
        expect(file.requiredVariables[2]).toEqual("arequired3");
    });

    it('should return file with Required, when file has correct DeployRequired values, and explicitly enabled Keys', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Disabled": false,
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            }
                        ]
                    }
                }),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(1);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.hasRequired).toEqual(true);
        expect(file.requiredVariables.length).toEqual(3);
        expect(file.requiredVariables[0]).toEqual("arequired1");
        expect(file.requiredVariables[1]).toEqual("arequired2");
        expect(file.requiredVariables[2]).toEqual("arequired3");
    });

    it('should return file with Required, when file has correct DeployRequired values, and multiple Key sections', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            },
                            {
                                "Keys": [
                                    "arequired4",
                                    "arequired5",
                                    "arequired6"]
                            }
                        ]
                    }
                }),
            writeAppSettingsFile: jest.fn(),
        };

        const file = await SettingsFile.create(reader, "apath");

        expect(file.path).toEqual("apath");
        expect(file.variables.length).toEqual(1);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.hasRequired).toEqual(true);
        expect(file.requiredVariables.length).toEqual(6);
        expect(file.requiredVariables[0]).toEqual("arequired1");
        expect(file.requiredVariables[1]).toEqual("arequired2");
        expect(file.requiredVariables[2]).toEqual("arequired3");
        expect(file.requiredVariables[3]).toEqual("arequired4");
        expect(file.requiredVariables[4]).toEqual("arequired5");
        expect(file.requiredVariables[5]).toEqual("arequired6");
    })
});
describe('substitute', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should not substitute, when no GitHub variables or secrets', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue({"aname": "avalue"}),
            writeAppSettingsFile: jest.fn(),
        };
        const file = await SettingsFile.create(reader, "apath");

        await file.substitute(logger, {}, {});

        expect(file.path).toEqual("apath");
        expect(reader.writeAppSettingsFile).toHaveBeenCalledWith("apath", {"aname": "avalue"});
        expect(logger.info).not.toHaveBeenCalledWith(SettingsFileMessages.substitutingVariable("aname"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, whenGitHub variables exists', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue({"aname": "avalue1"}),
            writeAppSettingsFile: jest.fn(),
        };
        const file = await SettingsFile.create(reader, "apath");

        await file.substitute(logger, {"ANAME": "avalue2"}, {});

        expect(file.path).toEqual("apath");
        expect(reader.writeAppSettingsFile).toHaveBeenCalledWith("apath", {"aname": "avalue2"});
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingVariable("aname"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, whenGitHub secret exists', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue({"aname": "avalue1"}),
            writeAppSettingsFile: jest.fn(),
        };
        const file = await SettingsFile.create(reader, "apath");

        await file.substitute(logger, {}, {"ANAME": "avalue2"});

        expect(file.path).toEqual("apath");
        expect(reader.writeAppSettingsFile).toHaveBeenCalledWith("apath", {"aname": "avalue2"});
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingVariable("aname"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, whenGitHub variable exists for deep traverse', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue({
                "alevel1": {
                    "alevel2": {
                        "alevel3": "avalue1"
                    }
                }
            }),
            writeAppSettingsFile: jest.fn(),
        };
        const file = await SettingsFile.create(reader, "apath");

        await file.substitute(logger, {}, {"ALEVEL1_ALEVEL2_ALEVEL3": "avalue2"});

        expect(file.path).toEqual("apath");
        expect(reader.writeAppSettingsFile).toHaveBeenCalledWith("apath", {
            "alevel1": {
                "alevel2": {
                    "alevel3": "avalue2"
                }
            }
        });
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingVariable("alevel1:alevel2:alevel3"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingSucceeded("apath"));
    })


    it('should substitute and redact Deploy node, whenGitHub variable exists', async () => {

        const reader: jest.Mocked<IAppSettingsJsonFileReaderWriter> = {
            readAppSettingsFile: jest.fn().mockResolvedValue({
                "alevel1": {
                    "alevel2": {
                        "alevel3": "avalue1"
                    }
                },
                "Deploy": {
                    "Required": [
                        {
                            "Keys": [
                                "arequired1",
                                "arequired2",
                                "arequired3"]
                        }
                    ]
                }
            }),
            writeAppSettingsFile: jest.fn(),
        };
        const file = await SettingsFile.create(reader, "apath");

        await file.substitute(logger, {}, {"ALEVEL1_ALEVEL2_ALEVEL3": "avalue2"});

        expect(file.path).toEqual("apath");
        expect(reader.writeAppSettingsFile).toHaveBeenCalledWith("apath", expect.objectContaining({
            "alevel1": {
                "alevel2": {
                    "alevel3": "avalue2"
                }
            },
            "Deploy": expect.stringContaining("All keys substituted, and removed:")
        }));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingVariable("alevel1:alevel2:alevel3"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileMessages.substitutingSucceeded("apath"));
    })

});
    
    