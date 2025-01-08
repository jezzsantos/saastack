import fs from "node:fs";

export interface IFileReaderWriter {
    readSettingsFile(path: string): Promise<any | string>;

    writeSettingsFile(path: string, content: any | string): Promise<any>;
}

export class AppSettingsJsonFileReaderWriter implements IFileReaderWriter {
    async writeSettingsFile(path: string, content: any): Promise<any> {
        try {
            const data = JSON.stringify(content, null, 2);
            await fs.promises.writeFile(path, data);
        } catch (error) {
            throw new Error(FileReaderWriterMessages.fileCannotBeWritten(path));
        }
    }

    async readSettingsFile(path: string): Promise<string> {
        let data: any;
        try {
            const result = await fs.promises.readFile(path);
            data = Buffer.from(result);
        } catch (error) {
            throw new Error(FileReaderWriterMessages.fileCannotBeRead(path));
        }

        const raw = data.toString();
        try {
            return JSON.parse(raw);
        } catch (error) {
            throw new Error(FileReaderWriterMessages.fileDoesNotContainValidJson(path, error));
        }
    }
}

export class FlatFileReaderWriter implements IFileReaderWriter {
    async writeSettingsFile(path: string, content: any): Promise<any> {
        try {
            await fs.promises.writeFile(path, content);
        } catch (error) {
            throw new Error(FileReaderWriterMessages.fileCannotBeWritten(path));
        }
    }

    async readSettingsFile(path: string): Promise<any> {
        let data: any;
        try {
            const result = await fs.promises.readFile(path);
            data = Buffer.from(result);
        } catch (error) {
            throw new Error(FileReaderWriterMessages.fileCannotBeRead(path));
        }

        return data.toString();
    }
}

export class FileReaderWriterMessages {
    public static fileCannotBeRead = (path: string): string => `File '${path}' cannot be read from disk, possibly it does not exist, or is not accessible?`;
    public static fileCannotBeWritten = (path: string): string => `File '${path}' cannot be written to disk, possibly it is not accessible?`;
    public static fileDoesNotContainValidJson = (path: string, error: any): string => `File '${path}' does not contain valid JSON: ${error}`;
}