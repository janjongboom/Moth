using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using BoneSoft.CSS;
using Moth.Core.ActionResults;
using Moth.Core.Externals;
using Moth.Core.Filters;
using Moth.Core.Helpers;
using Moth.Core.Providers;
using Yahoo.Yui.Compressor;

namespace Moth.Core
{
    [MothAction(DisableMoth = true)]
    public class ResourcesController : Controller
    {
        private readonly IOutputCacheProvider _provider;
        public ResourcesController()
        {
            _provider = MothAction.CacheProvider;
        }

        [HttpGet, FarFutureExpiration]
        public ActionResult Javascript(string keys)
        {
            string result = _provider.GetFromCache("resources.javascript." + Request.Url.PathAndQuery, () =>
            {
                var scriptFiles = keys.Split('|').ToList();

                var sb = new StringBuilder();

                foreach(var s in scriptFiles)
                {
                    var script = MothScriptHelper.GetFileContent(new List<string> { s }, HttpContext).ToString();
                    if (_provider.Enable.ScriptMinification)
                    {
                        script = JavaScriptCompressor.Compress(script, false, true, true, false, Int32.MaxValue, Encoding.UTF8, new CultureInfo("en-US"));
                    }
                    sb.AppendLine(script);
                }

                return sb.ToString();
            }, _provider.CacheDurations.ExternalScript);

            return new ContentResult()
            {
                Content = result,
                ContentType = "text/javascript",
            };
        }

        [HttpGet, FarFutureExpiration]
        public ActionResult Css(string keys)
        {
            var cssFiles = keys.Split('|').ToList();

            string result = _provider.GetFromCache("resources.css." + Request.Url.PathAndQuery, () =>
                {
                    var sb = new StringBuilder();
                    string stylos;

                    if (_provider.Enable.CssPreprocessing)
                    {
                        foreach (var f in cssFiles)
                        {
                            CSSDocument doc;
                            var definition = GenerateSpriteDefinition(f);

                            sb.AppendLine(definition.Document.ToOutput());
                        }
                        stylos = sb.ToString();
                    }
                    else
                    {
                        stylos = MothScriptHelper.GetFileContent(cssFiles, HttpContext).ToString();
                    }

                    if (_provider.Enable.ScriptMinification)
                    {
                        // minification
                        stylos = CssCompressor.Compress(stylos);
                    }
                    return stylos;
                }, _provider.CacheDurations.ExternalScript);


            return new ContentResult()
            {
                Content = result,
                ContentType = "text/css"
            };
        }

        [HttpGet, FarFutureExpiration]
        public ActionResult Sprites(string file, string key, string type)
        {
            var definition = GenerateSpriteDefinition(file);

            // we hebben hier nu de definitie van deze file; uithalen wat we nodig hebben
            var images = definition.Rules.Where(r => DataUriHelper.GetStringFromImageType(r.ImageFormat).Equals(type) && r.SpriteName.Equals(key)).ToList();

            // image bouwen
            Bitmap bmp = new Bitmap(images.Max(i => i.Width), images.Sum(i => i.Height));
            using (Graphics grp = Graphics.FromImage(bmp))
            {
                foreach (var img in images)
                {
                    using (MemoryStream ms = new MemoryStream(img.Bytes))
                    {
                        grp.DrawImage(new Bitmap(ms), new Point {X = img.X, Y = img.Y});
                    }
                }
            }

            return new ImageResult
                {
                    Image = bmp,
                    ImageFormat = ImageFormat.Png
                };
        }

        private SpriteRuleSet GenerateSpriteDefinition(string file)
        {
           return _provider.GetFromCache("spritedefinition." + file, () =>
               {
                    var parser = new BoneSoft.CSS.CSSParser();
                    string allText = System.IO.File.ReadAllText(HttpContext.Server.MapPath("~/" + file));
                   
                    if (_provider.Enable.CssTidy)
                    {
                        allText = new CssTidy().Tidy(HttpContext.Server.MapPath("~/" + file));
                    }

                    var doc = parser.ParseText(allText);

                    MurmurHash2UInt32Hack hashing = new MurmurHash2UInt32Hack();
                    uint hash = 0;

                    // check whether we have to sprite some images
                    List<SpriteRule> rules = new List<SpriteRule>();
                   List<RuleSet> spritesFromCss = doc.RuleSets.Where(d => d.Declarations.Any(r => r.Name.Equals("moth-sprite", StringComparison.OrdinalIgnoreCase))).ToList();
                   foreach (var sprite in spritesFromCss)
                    {
                        var spriteName = sprite.Declarations.First(r => r.Name == "moth-sprite");

                        Term term = GetImageTerm(sprite);

                        string localPath = term.Value;

                        var fullLocalPath = Path.Combine(HttpContext.Server.MapPath("~/" + Path.GetDirectoryName(file)), localPath);
                        var bytes = System.IO.File.ReadAllBytes(fullLocalPath);

                        var spriteRule = new SpriteRule
                            {
                                Bytes = bytes,
                                Filename = localPath,
                                SpriteName = spriteName.Expression.Terms.First().Value
                            };

                        using (var imageStream = new MemoryStream(bytes))
                        {
                            var bitmap = Image.FromStream(imageStream);

                            spriteRule.ImageFormat = bitmap.RawFormat;
                            spriteRule.Width = bitmap.Width;
                            spriteRule.Height = bitmap.Height;

                            //spriteRule.X = 0;
                            //spriteRule.Y = positionTable.ContainsKey(spriteRule.SpriteName) ? positionTable[spriteRule.SpriteName] : 0;
                        }

                        /*if (!positionTable.ContainsKey(spriteRule.SpriteName))
                            positionTable.Add(spriteRule.SpriteName, 0);

                        positionTable[spriteRule.SpriteName] += spriteRule.Height;

                        sprite.Declarations.Add(new Declaration {Name = "background-position-x", Expression = new Expression { Terms = new List<Term>{ new Term { Value = spriteRule.X + "px"}}}});
                        sprite.Declarations.Add(new Declaration { Name = "background-position-y", Expression = new Expression { Terms = new List<Term> { new Term { Value = -spriteRule.Y + "px" } } } });
                       */

                        sprite.Declarations.Add(new Declaration {Name = "moth-original-filename", Expression = new Expression {Terms = new List<Term> {new Term {Value = localPath}}}});

                        term.Value = string.Format("../../resources/sprites/?file={0}&key={1}&type={2}", file, spriteName.Expression, DataUriHelper.GetStringFromImageType(spriteRule.ImageFormat));

                        hash ^= hashing.Hash(bytes);

                        rules.Add(spriteRule);
                    }

                   // sort sprites
                   var blah = rules.ToArray();
                   new SpriteAlghoritms().AlgSortByArea(1200, blah);

                   rules = blah.ToList();

                   // add hash to every call
                   foreach(var sprite in spritesFromCss)
                   {
                       var term = GetImageTerm(sprite);
                       term.Value += string.Format("&{0}", hash);

                       var spriteRule = rules.First(r=>r.Filename == sprite.Declarations.First(f => f.Name == "moth-original-filename").Expression.Terms.First().Value);

                       // set position-x and position-y
                       sprite.Declarations.Add(new Declaration { Name = "background-position-x", Expression = new Expression { Terms = new List<Term> { new Term { Value = spriteRule.X + "px" } } } });
                       sprite.Declarations.Add(new Declaration { Name = "background-position-y", Expression = new Expression { Terms = new List<Term> { new Term { Value = -spriteRule.Y + "px" } } } });

                       // remove moth- extensions
                       sprite.Declarations = sprite.Declarations.Where(s => !s.Name.StartsWith("moth-")).ToList();
                   }

                    return new SpriteRuleSet
                        {
                            Hash = hash.ToString(),
                            Document = doc,
                            Rules = rules
                        };
                }, _provider.CacheDurations.ExternalScript);
        }

        private Term GetImageTerm(RuleSet sprite)
        {
            Term term;
            if (sprite.Declarations.Any(d => d.Name == "background-image"))
            {
                term = sprite.Declarations.First(d => d.Name == "background-image").Expression.Terms.First();
            }
            else
            {
                term = sprite.Declarations.First(d => d.Name == "background").Expression.Terms.First(t => t.Type == BoneSoft.CSS.TermType.Url);
            }
            return term;
        }
    }

    public class SpriteRuleSet
    {
        public string Hash { get; set; }
        public List<SpriteRule> Rules { get; set; }
        public CSSDocument Document { get; set; }
    }

    public class SpriteRule : ISpriteRectangle
    {
        public string Filename { get; set; }
        public string SpriteName { get; set; }
        public byte[] Bytes { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public ImageFormat ImageFormat { get; set; }
    }
}
