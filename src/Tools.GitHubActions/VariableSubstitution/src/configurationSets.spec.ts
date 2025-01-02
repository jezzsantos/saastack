import {ConfigurationSets} from "./configurationSets";
import {ILogger} from "./logger";
import {IGlobPatternParser} from "./globPatternParser";

describe('ConfigurationSets', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should warn and be empty when constructed with no files', async () => {

        const globParser: jest.Mocked<IGlobPatternParser> = {
            parseFiles: jest.fn(matches => Promise.resolve([])),
        };

        const sets = await ConfigurationSets.create(logger, globParser, '');

        expect(sets.hasNone).toBe(true);
        expect(globParser.parseFiles).toHaveBeenCalledWith([]);
        expect(logger.warning).toHaveBeenCalledWith('No settings files found in this repository, using the glob patterns: ');
    });

    it('should create a single set, when has one file at the root', async () => {

        const globParser: jest.Mocked<IGlobPatternParser> = {
            parseFiles: jest.fn(matches => Promise.resolve(["afile.json"])),
        };

        const sets = await ConfigurationSets.create(logger, globParser, '');

        expect(sets.hasNone).toBe(false);
        expect(sets.length).toBe(1);
        expect(sets.sets[0].HostProjectPath).toEqual(".");
        expect(sets.sets[0].SettingFiles).toEqual(["afile.json"]);
        expect(sets.sets[0].RequiredVariables).toEqual([]);
        expect(globParser.parseFiles).toHaveBeenCalledWith([]);
        expect(logger.info).toHaveBeenCalledWith('Found settings files:\n\t.:\n\t\tafile.json');
    });

    it('should create a single set, when has one file in a directory', async () => {

        const globParser: jest.Mocked<IGlobPatternParser> = {
            parseFiles: jest.fn(matches => Promise.resolve(["apath/afile.json"])),
        };

        const sets = await ConfigurationSets.create(logger, globParser, '');

        expect(sets.hasNone).toBe(false);
        expect(sets.length).toBe(1);
        expect(sets.sets[0].HostProjectPath).toEqual("apath");
        expect(sets.sets[0].SettingFiles).toEqual(["apath/afile.json"]);
        expect(sets.sets[0].RequiredVariables).toEqual([]);
        expect(globParser.parseFiles).toHaveBeenCalledWith([]);
        expect(logger.info).toHaveBeenCalledWith('Found settings files:\n\tapath:\n\t\tafile.json');
    });

    it('should create a single set, when has many files in same directory', async () => {

        const globParser: jest.Mocked<IGlobPatternParser> = {
            parseFiles: jest.fn(matches => Promise.resolve(["apath/afile1.json", "apath/afile2.json"])),
        };

        const sets = await ConfigurationSets.create(logger, globParser, '');

        expect(sets.hasNone).toBe(false);
        expect(sets.length).toBe(1);
        expect(sets.sets[0].HostProjectPath).toEqual("apath");
        expect(sets.sets[0].SettingFiles).toEqual(["apath/afile1.json", "apath/afile2.json"]);
        expect(sets.sets[0].RequiredVariables).toEqual([]);
        expect(globParser.parseFiles).toHaveBeenCalledWith([]);
        expect(logger.info).toHaveBeenCalledWith('Found settings files:\n\tapath:\n\t\tafile1.json,\n\t\tafile2.json');
    });
});