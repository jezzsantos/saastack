namespace System.Net.Http
{
  /// <summary>
  /// HACK: We define this enum here as a workaround, since <see cref="HttpMethod.Patch"/> is not defined in netStandard20
  /// </summary>
  public enum HttpMethod
  {
    Get,
    Delete,
    Post,
    Put,
    Patch
  }
}
