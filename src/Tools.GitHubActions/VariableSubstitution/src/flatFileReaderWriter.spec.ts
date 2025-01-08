import {FileReaderWriterMessages, FlatFileReaderWriter} from "./fileReaderWriter";

describe('readSettingsFile', () => {

    it('should throw, when file does not exist', async () => {

        const reader = new FlatFileReaderWriter();

        try {
            await reader.readSettingsFile('nonexistent.txt');
        } catch (error) {
            expect(error.message).toMatch(FileReaderWriterMessages.fileCannotBeRead("nonexistent.txt"));
        }
    });

    it('should return file, when file exists, but empty', async () => {

        const reader = new FlatFileReaderWriter();
        const path = `${__dirname}/testing/__data/emptyflatfile.txt`;

        const file = await reader.readSettingsFile(path);

        expect(file).toEqual("");
    });

    it('should return file, when file exists', async () => {

        const reader = new FlatFileReaderWriter();
        const path = `${__dirname}/testing/__data/validflatfile.txt`;

        const file = await reader.readSettingsFile(path);

        expect(file).toEqual("aname=avalue");
    });
});

describe('writeSettingsFile', () =>
    it('should write, when file exists', async () => {

        const reader = new FlatFileReaderWriter();
        const path = `${__dirname}/testing/__data/validflatfile.txt`;

        await reader.writeSettingsFile(path, "aname=avalue");
    }));