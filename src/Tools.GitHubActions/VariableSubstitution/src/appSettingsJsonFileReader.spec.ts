import {AppSettingsJsonFileReader, AppSettingsJsonFileReaderErrors} from "./appSettingsJsonFileReader";

describe('AppSettingsJsonFileReader', () => {

    it('should throw, when file does not exist', async () => {

        const reader = new AppSettingsJsonFileReader();

        try {
            await reader.readAppSettingsFile('nonexistent.json');
        } catch (error) {
            expect(error.message).toMatch(AppSettingsJsonFileReaderErrors.fileCannotBeRead("nonexistent.json"));
        }
    });

    it('should throw, when file is not JSON', async () => {

        const reader = new AppSettingsJsonFileReader();
        const path = `${__dirname}/testing/__data/invalidjson.txt`;

        try {
            await reader.readAppSettingsFile(path);
        } catch (error) {
            // HACK: We have a slightly different error message in CI builds!
            expect(error.message).toContain(`File '${path}' does not contain valid JSON: SyntaxError: Unexpected token`);
        }
    });

    it('should return file, when file has no variables', async () => {

        const reader = new AppSettingsJsonFileReader();
        const path = `${__dirname}/testing/__data/emptyjson.json`;

        const file = await reader.readAppSettingsFile(path);

        expect(file).toEqual({});
    });
});