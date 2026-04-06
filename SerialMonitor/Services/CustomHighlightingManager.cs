using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace SerialMonitor.Services;

public static class CustomHighlightingManager
{
    public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
    public static string DirectoryHighlighting => Path.Combine(BaseDirectory, "Highlighting");

    public static string CreateDirectory()
    {
        if (!Directory.Exists(DirectoryHighlighting)) Directory.CreateDirectory(DirectoryHighlighting);

        return DirectoryHighlighting;
    }

    public static void RegisterAllHighlightings()
    {
        RegisterHighlighting("LOG.xshd", "LOG", ".log");
    }

    public static void RegisterHighlighting(string fileName, string name, string extension)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        var resourceName = resourceNames.FirstOrDefault(resName => resName.EndsWith(fileName));

        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new XmlTextReader(stream);
                var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting(name, new[] { extension }, definition);
            }
        }
        else
        {
            var filePath = Path.Combine(CreateDirectory(), fileName);

            if (File.Exists(filePath))
            {
                using var reader = new XmlTextReader(filePath);
                var definition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                HighlightingManager.Instance.RegisterHighlighting(name, new[] { extension }, definition);
            }
            else
            {
                var defaultContent = GetDefaultXshdContent(name);
                File.WriteAllText(filePath, defaultContent);
            }
        }
    }

    public static IHighlightingDefinition? GetHighlightingByExtension(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".vmt" => HighlightingManager.Instance.GetDefinition("VMT"),
            ".qc" => HighlightingManager.Instance.GetDefinition("QC"),
            ".smd" => HighlightingManager.Instance.GetDefinition("SMD"),
            ".config" => HighlightingManager.Instance.GetDefinition("CONFIG"),
            _ => null
        };
    }

    private static string GetDefaultXshdContent(string name)
    {
        return name switch
        {
            "QC" => """
                    <?xml version="1.0"?>
                    <SyntaxDefinition name="QC" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
                      <Color name="Comment" foreground="Green" />
                      <Color name="Keyword" foreground="Blue" fontWeight="bold" />
                      <Color name="String" foreground="DarkRed" />
                      <Color name="Number" foreground="DarkMagenta" />
                      
                      <RuleSet>
                        <Span color="Comment" begin="//" />
                        <Span color="String">
                          <Begin>"</Begin>
                          <End>"</End>
                        </Span>
                        
                        <Keywords color="Keyword">
                          <Word>$modelname</Word>
                          <Word>$body</Word>
                          <Word>$sequence</Word>
                          <Word>$staticprop</Word>
                        </Keywords>
                        
                        <Rule color="Number">
                          \b\d+(\.\d+)?\b
                        </Rule>
                      </RuleSet>
                    </SyntaxDefinition>
                    """,

            "SMD" => """
                     <?xml version="1.0"?>
                     <SyntaxDefinition name="SMD" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
                       <Color name="Comment" foreground="Green" />
                       <Color name="Section" foreground="Blue" fontWeight="bold" />
                       <Color name="Number" foreground="DarkMagenta" />
                       
                       <RuleSet>
                         <Span color="Comment" begin="//" />
                         
                         <Keywords color="Section">
                           <Word>version</Word>
                           <Word>nodes</Word>
                           <Word>skeleton</Word>
                           <Word>triangles</Word>
                           <Word>end</Word>
                         </Keywords>
                         
                         <Rule color="Number">
                           -?\b\d+(\.\d+)?\b
                         </Rule>
                       </RuleSet>
                     </SyntaxDefinition>
                     """,

            "CONFIG" => """
                        <?xml version="1.0"?>
                        <SyntaxDefinition name="CONFIG" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
                          <Color name="Comment" foreground="Green" />
                          <Color name="Key" foreground="Blue" fontWeight="bold" />
                          <Color name="String" foreground="DarkRed" />
                          <Color name="Boolean" foreground="DarkMagenta" />
                          
                          <RuleSet>
                            <Span color="Comment" begin="#" />
                            
                            <Rule color="Key">
                              ^[a-zA-Z_][a-zA-Z0-9_]*(?=\s*=)
                            </Rule>
                            
                            <Keywords color="Boolean">
                              <Word>true</Word>
                              <Word>false</Word>
                            </Keywords>
                            
                            <Span color="String">
                              <Begin>"</Begin>
                              <End>"</End>
                            </Span>
                          </RuleSet>
                        </SyntaxDefinition>
                        """,

            _ => """
                 <?xml version="1.0"?>
                 <SyntaxDefinition name="Default" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
                   <Color name="Comment" foreground="Green" />
                   <Color name="String" foreground="DarkRed" />
                   
                   <RuleSet>
                     <Span color="Comment" begin="//" />
                     <Span color="String">
                       <Begin>"</Begin>
                       <End>"</End>
                     </Span>
                   </RuleSet>
                 </SyntaxDefinition>
                 """
        };
    }
}