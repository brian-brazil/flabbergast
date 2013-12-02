[CCode(cheader_filename = "config.h")]
namespace Package {
	[CCode(cname = "PACKAGE_VERSION")]
	public const string VERSION;
	[CCode(cname = "PACKAGE_BUGREPORT")]
	public const string BUGREPORT;
	[CCode(cname = "PACKAGE_NAME")]
	public const string NAME;
	[CCode(cname = "PACKAGE_STRING")]
	public const string STRING;
	[CCode(cname = "PACKAGE_URL")]
	public const string URL;
}
