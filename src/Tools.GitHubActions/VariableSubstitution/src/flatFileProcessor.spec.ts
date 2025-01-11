import {ILogger} from "./logger";
import {AppSettingRequiredVariable, SettingsFileProcessorMessages} from "./settingsFileProcessor";
import {IFileReaderWriter} from "./fileReaderWriter";
import {FlatFileProcessor} from "./flatFileProcessor";
import {WarningOptions} from "./main";

describe('getVariables', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should return empty, when file is empty', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue(""),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual([]);
        expect(result.requiredVariables).toEqual([]);
    });

    it('should return variables without required variables, when file is has a single variable', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname=avalue"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual(["aname"]);
        expect(result.requiredVariables).toEqual([]);
    });

    it('should return variables without required variables, when file is has a single variable with no value', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname="),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual(["aname"]);
        expect(result.requiredVariables).toEqual([]);
    });

    it('should return variables without required variables, when file is has multiple variables', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname1=avalue1\naname2=avalue2\naname3=avalue3"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual(["aname1", "aname2", "aname3"]);
        expect(result.requiredVariables).toEqual([]);
    });

    it('should return variables with required variables, when file is has single substitution', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname1=#{ANAME}"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual(["aname1"]);
        expect(result.requiredVariables).toEqual([new AppSettingRequiredVariable("aname1", "ANAME")]);
    });

    it('should return variables with required variables, when file is has single substitution with unusual characters', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname1=#{ANAME1.ANAME2-ANAME3:ANAME4;ANAME5(ANAME6)[ANAME7]=ANAME8=}"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual(["aname1"]);
        expect(result.requiredVariables).toEqual([new AppSettingRequiredVariable("aname1", "ANAME1_ANAME2_ANAME3_ANAME4_ANAME5_ANAME6__ANAME7__ANAME8_")]);
    });

    it('should return variables with required variables, when file is has single substitution, in many values', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname1=avalue1\naname2=#{ANAME2}\naname3=avalue3"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual(["aname1", "aname2", "aname3"]);
        expect(result.requiredVariables).toEqual([new AppSettingRequiredVariable("aname2", "ANAME2")]);
    });

    it('should return variables with required variables, when file is has many substitutions', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname1=#{ANAME1}\naname2=#{ANAME2}\naname3==#{ANAME3}"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.getVariables("apath");

        expect(result.variables).toEqual(["aname1", "aname2", "aname3"]);
        expect(result.requiredVariables).toEqual([new AppSettingRequiredVariable("aname1", "ANAME1"), new AppSettingRequiredVariable("aname2", "ANAME2"), new AppSettingRequiredVariable("aname3", "ANAME3")]);
    });
});

describe('substitute', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should not substitute, when no GitHub variables or secrets', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockResolvedValue("aname=avalue"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

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
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"aname": "avalue"}, {});

        expect(result).toEqual(false);
        expect(readerWriter.readSettingsFile).toHaveBeenCalledWith("apath");
        expect(readerWriter.writeSettingsFile).not.toHaveBeenCalled();
        expect(logger.info).not.toHaveBeenCalled();
        expect(logger.error).toHaveBeenCalledWith(SettingsFileProcessorMessages.unknownError("apath", new Error("amessage")));
    });

    it('should not substitute, when write file throws', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname=avalue"),
            writeSettingsFile: jest.fn().mockRejectedValue(new Error("amessage")),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"aname": "avalue"}, {});

        expect(result).toEqual(false);
        expect(readerWriter.readSettingsFile).toHaveBeenCalledWith("apath");
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname=avalue");
        expect(logger.info).not.toHaveBeenCalled();
        expect(logger.error).toHaveBeenCalledWith(SettingsFileProcessorMessages.unknownError("apath", new Error("amessage")));
    });

    it('should not substitute, when substitution expression not exist', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname=avalue"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"ANAME": "avalue1"}, {});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname=avalue");
        expect(logger.info).not.toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("aname"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));

    });

    it('should substitute, when GitHub variables exists', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname=#{ANAME}"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"ANAME": "avalue1"}, {});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname=avalue1");
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, when GitHub secret exists', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname=#{ANAME}"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {}, {"ANAME": "avalue1"});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname=avalue1");
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, when substitution expression uses AppSettingsJson name', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname=\"#{Level1:Level2:Level3}\""),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"LEVEL1_LEVEL2_LEVEL3": "avalue1"}, {});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname=\"avalue1\"");
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("Level1:Level2:Level3"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, when substitution expression is quoted', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname=\"#{ANAME}\""),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {"ANAME": "avalue1"}, {});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname=\"avalue1\"");
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, when multiple quoted substitutions', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname1=\"#{ANAME1}\"\naname2=\"#{ANAME2}\"\naname3=\"#{ANAME3}\""),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {
            "ANAME1": "avalue1",
            "ANAME2": "avalue2",
            "ANAME3": "avalue3",
        }, {});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname1=\"avalue1\"\naname2=\"avalue2\"\naname3=\"avalue3\"");
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME1"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME2"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME3"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });

    it('should substitute, when multiple naked substitutions', async () => {

        const readerWriter: jest.Mocked<IFileReaderWriter> = {
            readSettingsFile: jest.fn().mockReturnValue("aname1=#{ANAME1}\naname2=#{ANAME2}\naname3=#{ANAME3}"),
            writeSettingsFile: jest.fn(),
        };
        const processor = new FlatFileProcessor(logger, readerWriter, "apath");

        const result = await processor.substitute(new WarningOptions(), {
            "ANAME1": "avalue1",
            "ANAME2": "avalue2",
            "ANAME3": "avalue3",
        }, {});

        expect(result).toEqual(true);
        expect(readerWriter.writeSettingsFile).toHaveBeenCalledWith("apath", "aname1=avalue1\naname2=avalue2\naname3=avalue3");
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME1"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME2"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingVariable("ANAME3"));
        expect(logger.info).toHaveBeenCalledWith(SettingsFileProcessorMessages.substitutingSucceeded("apath"));
    });
});
    
    