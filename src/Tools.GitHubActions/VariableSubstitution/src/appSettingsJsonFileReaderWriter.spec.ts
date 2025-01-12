import {AppSettingsJsonFileReaderWriter, FileReaderWriterMessages} from "./fileReaderWriter";

describe('readSettingsFile', () => {

    it('should throw, when file does not exist', async () => {

        const reader = new AppSettingsJsonFileReaderWriter();

        try {
            await reader.readSettingsFile('nonexistent.json');
        } catch (error) {
            expect(error.message).toMatch(FileReaderWriterMessages.fileCannotBeRead("nonexistent.json"));
        }
    });

    it('should throw, when file is not JSON', async () => {

        const reader = new AppSettingsJsonFileReaderWriter();
        const path = `${__dirname}/testing/__data/invalidjson.txt`;

        try {
            await reader.readSettingsFile(path);
        } catch (error) {
            // HACK: We have a slightly different error message in CI builds!
            expect(error.message).toContain(`File '${path}' does not contain valid JSON: SyntaxError: Unexpected token`);
        }
    });

    it('should return file, when file exists, but empty', async () => {

        const reader = new AppSettingsJsonFileReaderWriter();
        const path = `${__dirname}/testing/__data/emptyjson.json`;

        const file = await reader.readSettingsFile(path);

        expect(file).toEqual({});
    });

    it('should return file, when file exists', async () => {

        const reader = new AppSettingsJsonFileReaderWriter();
        const path = `${__dirname}/testing/__data/validjson.json`;

        const file = await reader.readSettingsFile(path);

        expect(file).toEqual({"aname": "avalue"});
    });
});

describe('writeSettingsFile', () =>
    it('should write, when file exists', async () => {

        const reader = new AppSettingsJsonFileReaderWriter();
        const path = `${__dirname}/testing/__data/validjson.json`;

        await reader.writeSettingsFile(path, {"aname": "avalue"});
    }));