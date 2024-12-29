import fs from "node:fs";

export interface IAppSettingsJsonFileReaderWriter {
    readAppSettingsFile(path: string): Promise<any>;

    writeAppSettingsFile(path: string, json: any): Promise<any>;
}

export class AppSettingsJsonFileReaderWriter implements IAppSettingsJsonFileReaderWriter {
    async writeAppSettingsFile(path: string, json: any): Promise<any> {
        try {
            const data = JSON.stringify(json, null, 2);
            await fs.promises.writeFile(path, data);
        } catch (error) {
            throw new Error(AppSettingsJsonFileReaderMessages.fileCannotBeWritten(path));
        }
    }

    async readAppSettingsFile(path: string): Promise<any> {
        let data: any;
        try {
            const result = await fs.promises.readFile(path);
            data = Buffer.from(result);
        } catch (error) {
            throw new Error(AppSettingsJsonFileReaderMessages.fileCannotBeRead(path));
        }

        const raw = data.toString();
        try {
            return JSON.parse(raw);
        } catch (error) {
            throw new Error(AppSettingsJsonFileReaderMessages.fileDoesNotContainValidJson(path, error));
        }
    }
}

export class AppSettingsJsonFileReaderMessages {
    public static fileCannotBeRead = (path: string): string => `File '${path}' cannot be read from disk, possibly it does not exist, or is not accessible?`;
    public static fileCannotBeWritten = (path: string): string => `File '${path}' cannot be written to disk, possibly it is not accessible?`;
    public static fileDoesNotContainValidJson = (path: string, error: any): string => `File '${path}' does not contain valid JSON: ${error}`;
}