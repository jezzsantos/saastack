import fs from "node:fs";

export interface IAppSettingsJsonFileReader {
    readAppSettingsFile(path: string): Promise<any>;
}

export class AppSettingsJsonFileReader implements IAppSettingsJsonFileReader {
    async readAppSettingsFile(path: string): Promise<any> {
        let data: any;
        try {
            const result = await fs.promises.readFile(path);
            data = Buffer.from(result);
        } catch (error) {
            throw new Error(`File '${path}' cannot be read from disk, possibly it does not exist, or is not accessible?`);
        }

        const raw = data.toString();
        try {
            return JSON.parse(raw);
        } catch (error) {
            throw new Error(`File '${path}' does not contain valid JSON: ${error}`);
        }
    }

}