jest.mock("axios", () => {
  const actualImpl = jest.requireActual("axios");

  return {
    ...actualImpl,
    request: jest.fn((config) => Promise.resolve({ config, data: {}, status: 200 }))
  };
});

jest.mock("@hey-api/client-axios", () => {
  const actualImpl = jest.requireActual("@hey-api/client-axios");

  return {
    ...actualImpl,
    createClient: jest.fn().mockImplementation((config) => {
      const clientImpl = actualImpl.createClient(config);
      return {
        ...clientImpl,
        get: jest.fn((config) => Promise.resolve({ config, data: {}, status: 200 })),
        post: jest.fn((config) => Promise.resolve({ config, data: {}, status: 201 })),
        put: jest.fn((config) => Promise.resolve({ config, data: {}, status: 202 })),
        delete: jest.fn((config) => Promise.resolve({ config, data: {}, status: 204 }))
      };
    })
  };
});

// we need to save the original object for later to not affect tests from other files
const ogLocation = global.window.location;

beforeAll(() => {
  process.env.WEBSITEHOSTBASEURL = "abaseurl";
  jest.spyOn(document, "querySelector").mockImplementation((selector) => {
    if (selector == "meta[name='csrf-token']") {
      return {
        getAttribute: jest.fn().mockReturnValue("acsrftoken")
      } as unknown as Element;
    }

    return null;
  });
  // @ts-ignore
  delete global.window.location;
  // noinspection JSConstantReassignment
  global.window = Object.create(window);
  // @ts-ignore
  global.window.location = { assign: jest.fn() };
});

afterAll(() => {
  global.window.location = ogLocation;
});
