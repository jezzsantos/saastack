import {ILogger} from "./logger";
import {AppSettingRequiredVariable, SettingsFileProcessorMessages} from "./settingsFileProcessor";
import {IFileReaderWriter} from "./fileReaderWriter";
import {AppSettingsJsonFileProcessor} from "./appSettingsJsonFileProcessor";
import {WarningOptions} from "./main";

describe('getVariables', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should return empty, when file is empty', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue({}),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual([]);
        expect(result.requiredVariables).toEqual([]);
    });

    it('should return variables without any required variables, when file has multi-level variables', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1.1": {
                        "Level2.1": "avalue1",
                        "Level2.2": {
                            "Level3.1": "avalue2"
                        }
                    },
                    "Level1.2": "avalue4"
                }),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables.length).toEqual(3);
        expect(result.variables[0]).toEqual("Level1.1:Level2.1");
        expect(result.variables[1]).toEqual("Level1.1:Level2.2:Level3.1");
        expect(result.variables[2]).toEqual("Level1.2");
    });

    it('should return variables without any required variables, when file has incorrectly typed DeployRequired value', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": "adeploy"
                }),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables.length).toEqual(2);
        expect(result.variables[0]).toEqual("Level1");
        expect(result.variables[1]).toEqual("Deploy");
    });

    it('should return variables without any required variables, when file has incorrectly nested DeployRequired value', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(
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
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables.length).toEqual(3);
        expect(result.variables[0]).toEqual("Level1:Deploy:Required:0:Keys:0");
        expect(result.variables[1]).toEqual("Level1:Deploy:Required:0:Keys:1");
        expect(result.variables[2]).toEqual("Level1:Deploy:Required:0:Keys:2");
    });

    it('should return variables without any required variables, when file has correct DeployRequired values, but explicitly disabled Keys', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(
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
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables.length).toEqual(1);
        expect(result.variables[0]).toEqual("Level1");
        expect(result.requiredVariables.length).toEqual(0);
    });

    it('should return variables with required variables, when file has correct DeployRequired values, and not explicitly disabled Keys', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(
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
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables.length).toEqual(1);
        expect(result.variables[0]).toEqual("Level1");
        expect(result.requiredVariables.length).toEqual(3);
        expect(result.requiredVariables[0]).toEqual(new AppSettingRequiredVariable("arequired1", "AREQUIRED1"));
        expect(result.requiredVariables[1]).toEqual(new AppSettingRequiredVariable("arequired2", "AREQUIRED2"));
        expect(result.requiredVariables[2]).toEqual(new AppSettingRequiredVariable("arequired3", "AREQUIRED3"));
    });

    it('should return variables with required variables, when file has correct DeployRequired values, and explicitly enabled Keys', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(
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
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables.length).toEqual(1);
        expect(result.variables[0]).toEqual("Level1");
        expect(result.requiredVariables.length).toEqual(3);
        expect(result.requiredVariables[0]).toEqual(new AppSettingRequiredVariable("arequired1", "AREQUIRED1"));
        expect(result.requiredVariables[1]).toEqual(new AppSettingRequiredVariable("arequired2", "AREQUIRED2"));
        expect(result.requiredVariables[2]).toEqual(new AppSettingRequiredVariable("arequired3", "AREQUIRED3"));
    });

    it('should return variables with required variables, when file has correct DeployRequired values, and multiple Key sections', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(
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
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables.length).toEqual(1);
        expect(result.variables[0]).toEqual("Level1");
        expect(result.requiredVariables.length).toEqual(6);
        expect(result.requiredVariables[0]).toEqual(new AppSettingRequiredVariable("arequired1", "AREQUIRED1"));
        expect(result.requiredVariables[1]).toEqual(new AppSettingRequiredVariable("arequired2", "AREQUIRED2"));
        expect(result.requiredVariables[2]).toEqual(new AppSettingRequiredVariable("arequired3", "AREQUIRED3"));
        expect(result.requiredVariables[3]).toEqual(new AppSettingRequiredVariable("arequired4", "AREQUIRED4"));
        expect(result.requiredVariables[4]).toEqual(new AppSettingRequiredVariable("arequired5", "AREQUIRED5"));
        expect(result.requiredVariables[5]).toEqual(new AppSettingRequiredVariable("arequired6", "AREQUIRED6"));
    })
});

describe('substitute', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should not substitute, when no GitHub variables or secrets', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue({"aname": "avalue"}),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {}, {});

        expect(result).toEqual(true);
        expect(readerWriter.readSettingsFile).not.toHaveBeenCalled();
        expect(logger.info).not.toHaveBeenCalled();
    });

    it('should not substitute, when read file throws', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockRejectedValue(new Error("amessage")),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"aname": "avalue"}, {});

        expect(result).toEqual(false);
        expect(readerWriter.readSettingsFile).toHaveBeenCalledWith("apath");
        expect(readerWriter.writeSettingsFile).not.toHaveBeenCalled();
        expect(logger.info).not.toHaveBeenCalled();
        expect(logger.error).toHaveBeenCalledWith(SettingsFileProcessorMessages.unknownError("apath", new Error("amessage")));
    });

    it('should not substitute, when write file throws', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue({"aname": "avalue"}),
            writeSettingsFile: jest.fn().mockRejectedValue(new Error("amessage")),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"aname": "avalue"}, {});

        expect(result).toEqual(false);
        expect(readerWriter.readSettingsFile).toHaveBeenCalledWith("apath");
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", {"aname": "avalue"});
        expect(logger.info).not.toHaveBeenCalled();
        expect(logger.error).toHaveBeenCalledWith(SettingsFileProcessorMessages.unknownError("apath", new Error("amessage")));
    });

    it('should substitute, when GitHub variables exists', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue({"aname": "avalue1"}),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"ANAME": "avalue2"}, {});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", {"aname": "avalue2"});
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("aname"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, when GitHub secret exists', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue({"aname": "avalue1"}),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {}, {"ANAME": "avalue2"});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", {"aname": "avalue2"});
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("aname"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, when GitHub variable exists for deep traverse', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue({
                "alevel1": {
                    "alevel2": {
                        "alevel3": "avalue1"
                    }
                }
            }),
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {}, {"ALEVEL1_ALEVEL2_ALEVEL3": "avalue2"});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", {
            "alevel1": {
                "alevel2": {
                    "alevel3": "avalue2"
                }
            }
        });
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("alevel1:alevel2:alevel3"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute and redact Deploy node, when GitHub variable exists', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue({
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
            writeSettingsFile: jest.fn(),
        };
        const processor = new AppSettingsJsonFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {}, {"ALEVEL1_ALEVEL2_ALEVEL3": "avalue2"});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", expect.objectContaining({
            "alevel1": {
                "alevel2": {
                    "alevel3": "avalue2"
                }
            },
            "Deploy": expect.stringContaining("All keys substituted, and removed:")
        }));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("alevel1:alevel2:alevel3"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    })
});
    
    