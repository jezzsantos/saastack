import {glob} from "glob";

export interface IGlobPatternParser {
    parseFiles(expressions: string[]): Promise<string[]>;
}

export class GlobPatternParser implements IGlobPatternParser {
    async parseFiles(expressions: string[]): Promise<string[]> {
        return await glob(expressions,
            {
                ignore: ['**/node_modules/**', '**/bin/**', '**/obj/**'],
                nodir: true,
            });
    }
}