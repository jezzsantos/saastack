import {SettingsFile} from "./settingsFile";
import {IAppSettingsJsonFileReader} from "./appSettingsJsonFileReader";

describe('SettingsFile', () => {
    it('should return file, when file has no variables', async () => {

        const path = `${__dirname}/testing/__data/emptyjson.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue({}),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(0);
    });

    it('should return file, when file has multi-level variables', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1.1": {
                        "Level2.1": "avalue1",
                        "Level2.2": {
                            "Level3.1": "avalue2"
                        }
                    },
                    "Level1.2": "avalue4"
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(3);
        expect(file.variables[0]).toEqual("Level1.1:Level2.1");
        expect(file.variables[1]).toEqual("Level1.1:Level2.2:Level3.1");
        expect(file.variables[2]).toEqual("Level1.2");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file without Required, when file has incorrectly typed Required value', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Required": "arequired"
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(2);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.variables[1]).toEqual("Required");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file without Required, when file has incorrectly nested Required value', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": {
                        "Required": [
                            "arequired1",
                            "arequired2",
                            "arequired3"
                        ]
                    }
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(3);
        expect(file.variables[0]).toEqual("Level1:Required:0");
        expect(file.variables[1]).toEqual("Level1:Required:1");
        expect(file.variables[2]).toEqual("Level1:Required:2");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file with Required, when file has correct Required values', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Required": [
                        "arequired1",
                        "arequired2",
                        "arequired3"
                    ]
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(1);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.hasRequired).toEqual(true);
        expect(file.requiredVariables.length).toEqual(3);
        expect(file.requiredVariables[0]).toEqual("arequired1");
        expect(file.requiredVariables[1]).toEqual("arequired2");
        expect(file.requiredVariables[2]).toEqual("arequired3");
    })
});