using System.Text;
using Gusecra.Models;
using NuGet.Frameworks;

namespace GusecraTests;

public class Tests
{
    private byte[] key = Encoding.UTF8.GetBytes("Test");
    private byte[] iv = Encoding.UTF8.GetBytes("test1234");

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestMalformed() 
    {
        var request = @"
<START>
MSG  wefwef
CALL wefwef
<STOP>
        ";
        
        try {
            RequestUtils.ParseRequest(request, key, iv);
            Assert.Fail();
        }catch(FormatException) {
        }

    }

    [Test]
    public void TestEncodeDecode()
    {
        // Short message
        var data = ecode("hello world");
        var encoded = RequestUtils.CreateRequest(
            new Request(true, data), 
            key, iv
        );
        var decoded = RequestUtils.ParseRequest(dcode(encoded), key, iv);
        Assert.That(dcode(data) == dcode(decoded.Message));
        
        // Long message
        data = ecode(new string('a', 1250));
        encoded = RequestUtils.CreateRequest(
            new Request(true, data), 
            key, iv
        );
        decoded = RequestUtils.ParseRequest(dcode(encoded), key, iv);
        Assert.That(dcode(data) == dcode(decoded.Message));
    }

    private string dcode(byte[] data) => Encoding.UTF8.GetString(data);
    private byte[] ecode(string data) => Encoding.UTF8.GetBytes(data);
}