# thrinax

A lib which is used of Chinese unstructured text capture.

## Download

    PM> Install-Package Thrinax

## Parser a list webpage

How to use?

    //get the html
    HttpResult httpResult = null;
    if (UseBrowser) //if need browser, you must install chrome first.
        httpResult = SeleniumHelper.HttpRequest(channelUrl);
    else
        httpResult = HttpHelper.HttpRequest(channelUrl);

    //parser the pattern
    var listPagePatterns = SmartParser.Extract_Patterns(channelUrl, httpResult.Content, MediaType.WebNews, Enums.Language.CHINESE);


You can get the demo from [Thrinax.HtmlListParserSample](https://github.com/ziyunhx/thrinax/tree/master/samples/Thrinax.HtmlListParserSample).

 ![list](https://www.tnidea.com/media/image/thrinax-2-01.png)

 ## Parser a content webpage

How to use?

    Article article = HtmlToArticle.GetArticle(htmlContent);

You can get the demo from [Thrinax.ExtractArticleSample](https://github.com/ziyunhx/thrinax/tree/master/samples/Thrinax.ExtractArticleSample).

![content](https://www.tnidea.com/media/image/thrinax-1-03.png)

## License

The Apache License 2.0 applies to all samples in this repository.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
