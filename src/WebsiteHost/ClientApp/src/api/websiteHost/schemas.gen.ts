// This file is auto-generated by @hey-api/openapi-ts

export const AuthenticateRequestSchema = {
  required: ["provider"],
  type: "object",
  properties: {
    authCode: {
      type: "string",
      nullable: true
    },
    password: {
      type: "string",
      nullable: true
    },
    provider: {
      minLength: 1,
      type: "string"
    },
    username: {
      type: "string",
      nullable: true
    }
  },
  additionalProperties: false
} as const;

export const AuthenticateResponseSchema = {
  type: "object",
  properties: {
    userId: {
      type: "string",
      nullable: true
    }
  },
  additionalProperties: false
} as const;

export const EmptyResponseSchema = {
  type: "object",
  additionalProperties: false
} as const;

export const FeatureFlagSchema = {
  type: "object",
  properties: {
    isEnabled: {
      type: "boolean"
    },
    name: {
      type: "string",
      nullable: true
    }
  },
  additionalProperties: false
} as const;

export const GetAllFeatureFlagsResponseSchema = {
  type: "object",
  properties: {
    flags: {
      type: "array",
      items: {
        $ref: "#/components/schemas/FeatureFlag"
      },
      nullable: true
    }
  },
  additionalProperties: false
} as const;

export const GetFeatureFlagResponseSchema = {
  type: "object",
  properties: {
    flag: {
      $ref: "#/components/schemas/FeatureFlag"
    }
  },
  additionalProperties: false
} as const;

export const HealthCheckResponseSchema = {
  type: "object",
  properties: {
    name: {
      type: "string",
      nullable: true
    },
    status: {
      type: "string",
      nullable: true
    }
  },
  additionalProperties: false
} as const;

export const LogoutRequestSchema = {
  type: "object",
  additionalProperties: false
} as const;

export const ProblemDetailsSchema = {
  type: "object",
  properties: {
    type: {
      type: "string",
      nullable: true
    },
    title: {
      type: "string",
      nullable: true
    },
    status: {
      type: "integer",
      format: "int32",
      nullable: true
    },
    detail: {
      type: "string",
      nullable: true
    },
    instance: {
      type: "string",
      nullable: true
    }
  },
  additionalProperties: {}
} as const;

export const RecordCrashRequestSchema = {
  required: ["message"],
  type: "object",
  properties: {
    message: {
      minLength: 1,
      type: "string"
    }
  },
  additionalProperties: false
} as const;

export const RecordMeasureRequestSchema = {
  required: ["eventName"],
  type: "object",
  properties: {
    additional: {
      type: "object",
      additionalProperties: {
        nullable: true
      },
      nullable: true
    },
    eventName: {
      minLength: 1,
      type: "string"
    }
  },
  additionalProperties: false
} as const;

export const RecordPageViewRequestSchema = {
  required: ["path"],
  type: "object",
  properties: {
    path: {
      minLength: 1,
      type: "string"
    }
  },
  additionalProperties: false
} as const;

export const RecordTraceRequestSchema = {
  required: ["level", "messageTemplate"],
  type: "object",
  properties: {
    arguments: {
      type: "array",
      items: {
        type: "string"
      },
      nullable: true
    },
    level: {
      minLength: 1,
      type: "string"
    },
    messageTemplate: {
      minLength: 1,
      type: "string"
    }
  },
  additionalProperties: false
} as const;

export const RecordUseRequestSchema = {
  required: ["eventName"],
  type: "object",
  properties: {
    additional: {
      type: "object",
      additionalProperties: {
        nullable: true
      },
      nullable: true
    },
    eventName: {
      minLength: 1,
      type: "string"
    }
  },
  additionalProperties: false
} as const;

export const RefreshTokenRequestSchema = {
  type: "object",
  additionalProperties: false
} as const;

export const VoidSchema = {
  type: "object",
  additionalProperties: false
} as const;
