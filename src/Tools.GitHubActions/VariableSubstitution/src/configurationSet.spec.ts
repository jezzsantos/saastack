import {ConfigurationSet} from "./configurationSet";
import {ILogger} from "./logger";
import {ISettingsFile} from "./settingsFile";

describe('verifyConfiguration', () => {
    const logger: jest.Mocked<ILogger> = {
        info: jest.fn(),
        warning: jest.fn(),
        error: jest.fn(),
    };

    it('should return true, when the set contains no required', async () => {

        const set = new ConfigurationSet("apath", []);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {});

        expect(result).toBe(true);
        expect(logger.info).toHaveBeenCalledWith(`\tVerifying settings files in host: 'apath' -> Successful!`);
    });

    it('should return true, but warn, when the set defines required variable, but no variable exists to substitute', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["aname"],
            requiredVariables: ["arequired"],
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {});

        expect(result).toBe(true);
        expect(logger.warning).toHaveBeenCalledWith(`\tThe following '1' Required variables are not yet defined in any of the settings files of this host! Consider either defining them in one of the settings files of this host, OR remove them from the 'Deploy -> Required -> Keys' section of the settings files in this host:\n\t\t1. arequired`);
        expect(logger.info).toHaveBeenCalledWith(`\tVerifying settings files in host: 'apath' -> Successful!`);
    });

    it('should return false, when the set defines required variable, but no variable/secret exists in GitHub', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["arequired-arequired:aname"],
            requiredVariables: ["arequired-arequired:aname"],
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {});

        expect(result).toBe(false);
        expect(logger.info).not.toHaveBeenCalledWith(`Verification of host 'apath' completed successfully`);
        expect(logger.error).toHaveBeenCalledWith(`\tThe following '1' Required GitHub environment variables (or secrets) have not been defined in the environment variables (or secrets) of this GitHub project:\n\t\t1. AREQUIRED_AREQUIRED_ANAME (alias: arequired-arequired:aname)`);
        expect(logger.error).toHaveBeenCalledWith(`\tVerification settings files in host: 'apath' -> Failed! there is at least one missing required environment variable or secret in this GitHub project`);
    });

    it('should return true, when the set defines required variable, and variable exists in GitHub', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["arequired-arequired:aname"],
            requiredVariables: ["arequired-arequired:aname"],
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {"AREQUIRED_AREQUIRED_ANAME": "avalue"}, {});

        expect(result).toBe(true);
        expect(logger.info).toHaveBeenCalledWith(`\tVerifying settings files in host: 'apath' -> Successful!`);
    });

    it('should return true, when the set defines required variable, and secret exists in GitHub', async () => {

        const settingsFile: jest.Mocked<ISettingsFile> = {
            path: "afile.json",
            hasRequired: true,
            variables: ["arequired-arequired:aname"],
            requiredVariables: ["arequired-arequired:aname"],
        };
        const set = new ConfigurationSet("apath", [settingsFile]);
        set.accumulateVariables();

        const result = set.verify(logger, {}, {"AREQUIRED_AREQUIRED_ANAME": "avalue"});

        expect(result).toBe(true);
        expect(logger.info).toHaveBeenCalledWith(`\tVerifying settings files in host: 'apath' -> Successful!`);
    });
});