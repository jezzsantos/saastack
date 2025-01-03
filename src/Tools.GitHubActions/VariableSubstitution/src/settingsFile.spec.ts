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

    it('should return file without Required, when file has incorrectly typed DeployRequired value', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": "adeploy"
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(2);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.variables[1]).toEqual("Deploy");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file without Required, when file has incorrectly nested DeployRequired value', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": {
                        "Deploy": {
                            "Required": [
                                {
                                    "Keys": [
                                        "arequired1",
                                        "arequired2",
                                        "arequired3"]
                                }
                            ]
                        }
                    }
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(3);
        expect(file.variables[0]).toEqual("Level1:Deploy:Required:0:Keys:0");
        expect(file.variables[1]).toEqual("Level1:Deploy:Required:0:Keys:1");
        expect(file.variables[2]).toEqual("Level1:Deploy:Required:0:Keys:2");
        expect(file.hasRequired).toEqual(false);
    });

    it('should return file without Required, when file has correct DeployRequired values, but explicitly disabled Keys', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Disabled": true,
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            }
                        ]
                    }
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(1);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.hasRequired).toEqual(false);
        expect(file.requiredVariables.length).toEqual(0);
    });

    it('should return file with Required, when file has correct DeployRequired values, and not explicitly disabled Keys', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            }
                        ]
                    }
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

    it('should return file with Required, when file has correct DeployRequired values, and explicitly enabled Keys', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Disabled": false,
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            }
                        ]
                    }
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
    });

    it('should return file with Required, when file has correct DeployRequired values, and multiple Key sections', async () => {

        const path = `${__dirname}/testing/__data/appsettings.json`;
        const reader: jest.Mocked<IAppSettingsJsonFileReader> = {
            readAppSettingsFile: jest.fn().mockResolvedValue(
                {
                    "Level1": "avalue",
                    "Deploy": {
                        "Required": [
                            {
                                "Keys": [
                                    "arequired1",
                                    "arequired2",
                                    "arequired3"]
                            },
                            {
                                "Keys": [
                                    "arequired4",
                                    "arequired5",
                                    "arequired6"]
                            }
                        ]
                    }
                }),
        };

        const file = await SettingsFile.create(reader, path);

        expect(file.path).toEqual(path);
        expect(file.variables.length).toEqual(1);
        expect(file.variables[0]).toEqual("Level1");
        expect(file.hasRequired).toEqual(true);
        expect(file.requiredVariables.length).toEqual(6);
        expect(file.requiredVariables[0]).toEqual("arequired1");
        expect(file.requiredVariables[1]).toEqual("arequired2");
        expect(file.requiredVariables[2]).toEqual("arequired3");
        expect(file.requiredVariables[3]).toEqual("arequired4");
        expect(file.requiredVariables[4]).toEqual("arequired5");
        expect(file.requiredVariables[5]).toEqual("arequired6");
    })
});
    