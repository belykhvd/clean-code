using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using FluentAssertions;

namespace Markdown
{
	public class Md
	{	    
        public string RenderToHtml(string markdown)
        {
            var markTagRendering = RenderMarks(markdown);
	        
	        var htmlBuilder = new StringBuilder(markdown.Length);
	        for (var i = 0; i < markdown.Length; i++)
	        {
	            var currentSymbol = markdown[i];
	            if (currentSymbol == '\\')
	            {
	                htmlBuilder.Append(markdown[++i]);
                    continue;	                
	            }

	            if (markTagRendering.TryGetValue(i, out var mapping))
	            {
	                htmlBuilder.Append(mapping.Tag);
	                i += mapping.Mark.Length - 1;
	            }
	            else
	            {
	                htmlBuilder.Append(currentSymbol);
	            }
	        }

	        return htmlBuilder.ToString();
	    }

	    private static Dictionary<int, MarkTagMapping> RenderMarks(string markdown)
	    {
	        var marksStack = new Stack<Tuple<MarkTagMapping, int>>();
	        var marksRendering = new Dictionary<int, MarkTagMapping>();

            for (var i = 0; i < markdown.Length; i++)
            {
                var nextMapping = TryGetNextMapping(marksStack, markdown, i);

                if (nextMapping == null)
                    continue;

                if (marksStack.Count >= 1 && TryRenderPair(marksRendering, marksStack, nextMapping))
                {
                    marksStack.Pop();
                    continue;                   
                }

                marksStack.Push(nextMapping);
                i += nextMapping.Item1.Mark.Length - 1;
            }

	        return marksRendering;
	    }

	    private static Tuple<MarkTagMapping, int> TryGetNextMapping(Stack<Tuple<MarkTagMapping, int>> marksStack,
            string markdown, int currentIndex)
	    {
	        Tuple<MarkTagMapping, int> nextMapping = null;

	        if (IsHeaderMark(markdown, currentIndex))
	        {
	            nextMapping = Tuple.Create(new MarkTagMapping("#", "h1"), currentIndex);
	        }
	        else if (CanBeOpenStrongMark(markdown, currentIndex))
	        {
	            nextMapping = CanBeCloseStrongMark(marksStack, markdown, currentIndex)
	                ? Tuple.Create(new MarkTagMapping("__", "strong", false), currentIndex)
	                : Tuple.Create(new MarkTagMapping("__", "strong"), currentIndex);
	        }
	        else if (CanBeOpenEmMark(markdown, currentIndex))
	        {
	            nextMapping = CanBeCloseEmMark(marksStack, markdown, currentIndex)
	                ? Tuple.Create(new MarkTagMapping("_", "em", false), currentIndex)
	                : Tuple.Create(new MarkTagMapping("_", "em"), currentIndex);
	        }
	        else if (CanBeCloseStrongMark(marksStack, markdown, currentIndex))
	        {
	            nextMapping = Tuple.Create(new MarkTagMapping("__", "strong", false), currentIndex);
	        }
	        else if (CanBeCloseEmMark(marksStack, markdown, currentIndex))
	        {
	            nextMapping = Tuple.Create(new MarkTagMapping("_", "em", false), currentIndex);
	        }

	        return nextMapping;
	    }

	    private static bool TryRenderPair(IDictionary<int, MarkTagMapping> marksRendering,
	        Stack<Tuple<MarkTagMapping, int>> marksStack, 
            Tuple<MarkTagMapping, int> nextMapping)
	    {
	        var topMapping = marksStack.Peek();
	        if (!nextMapping.Item1.IsPairWith(topMapping.Item1))
                return false;

	        marksRendering[topMapping.Item2] = topMapping.Item1;
	        marksRendering[nextMapping.Item2] = nextMapping.Item1;
	        return true;
	    }

	    private static bool IsHeaderMark(string markdown, int currentIndex)
	        => markdown[currentIndex] == '#';

	    private static bool CanBeOpenEmMark(string markdown, int currentIndex)
	    {
	        if (markdown[currentIndex] != '_' || currentIndex + 1 >= markdown.Length)
	            return false;

	        var nextSymbol = markdown[currentIndex + 1];
	        return !char.IsWhiteSpace(nextSymbol) && nextSymbol != '_';
	    }

	    private static bool CanBeCloseEmMark(Stack<Tuple<MarkTagMapping, int>> marksStack, string markdown, int currentIndex)
	    {
	        if (markdown[currentIndex] != '_' || currentIndex - 1 < 0)
	            return false;

	        if (currentIndex + 1 < markdown.Length && markdown[currentIndex + 1] == '_')
	            return false;
	        
            if (char.IsWhiteSpace(markdown[currentIndex - 1]))
                return false;

	        if (marksStack.Count == 0)
	            return false;

	        var topMapping = marksStack.Peek().Item1;
	        return topMapping.Mark == "_" && topMapping.IsOpen;
	    }

	    private static bool CanBeOpenStrongMark(string markdown, int currentIndex)
	    {
	        if (markdown[currentIndex] != '_' || currentIndex + 2 >= markdown.Length)
	            return false;

	        return markdown[currentIndex + 1] == '_'
	               && !char.IsWhiteSpace(markdown[currentIndex + 2])
	               && markdown[currentIndex + 2] != '_';
	    }

	    private static bool CanBeCloseStrongMark(Stack<Tuple<MarkTagMapping, int>> marksStack, string markdown, int currentIndex)
	    {
	        if (markdown[currentIndex] != '_' || currentIndex - 1 < 0 || currentIndex + 1 >= markdown.Length)
	            return false;

	        if (markdown[currentIndex + 1] != '_' || char.IsWhiteSpace(markdown[currentIndex - 1]))
	            return false;

	        if (marksStack.Count == 0)
	            return false;

	        var topMapping = marksStack.Peek().Item1;
            return topMapping.Mark == "__" && topMapping.IsOpen;
        }
	}

	[TestFixture]
	public class Md_ShouldRender
	{
        private static readonly Md Renderer = new Md();

	    [TestCase("_inner_", @"<em>inner<\em>", TestName = "DoesSupportEmTags")]
	    [TestCase("_ inner_", "_ inner_", TestName = "WhiteSpaceAfterOpenUnderscore")]
	    [TestCase("_inner _", "_inner _", TestName = "WhiteSpaceBeforeCloseUnderscore")]
	    [TestCase("__", "__", TestName = "EmptyUnderscores")]
	    [TestCase("_inner", "_inner", TestName = "NoCloseUnderscore")]
	    [TestCase("inner_", "inner_", TestName = "NoOpenUnderscore")]
	    [TestCase("_inner_ _inner_", @"<em>inner<\em> <em>inner<\em>", TestName = "MultipleMapping")]
	    [TestCase(" a b_inner_c d e_inner_f g ", @" a b<em>inner<\em>c d e<em>inner<\em>f g ", TestName = "LettersNoise")]
        [TestCase("_inner _inner_ inner_", @"<em>inner <em>inner<\em> inner<\em>", TestName = "OverlappingNoise")]
        [TestCase("_", "_", TestName = "Single underscore")]
        public void CanRenderEmTagsTests(string markdown, string expectedHtml)
	        => AssertEqualHtml(markdown, expectedHtml);

        [TestCase("__inner__", @"<strong>inner<\strong>", TestName = "DoesSupportStrongTags")]        
        [TestCase("__ inner__", "__ inner__", TestName = "WhiteSpaceAfterOpenDoubleUnderscore")]
        [TestCase("__inner __", "__inner __", TestName = "WhiteSpaceBeforeCloseDoubleUnderscore")]
        [TestCase("____", "____", TestName = "EmptyDoubleUnderscores")]
        [TestCase("__inner", "__inner", TestName = "NoCloseDoubleUnderscore")]
        [TestCase("inner__", "inner__", TestName = "NoOpenDoubleUnderscore")]
        [TestCase("__inner__ __inner__", @"<strong>inner<\strong> <strong>inner<\strong>", TestName = "MultipleMapping")]
        [TestCase(" a b__inner__c d e__inner__f g ", @" a b<strong>inner<\strong>c d e<strong>inner<\strong>f g ", TestName = "LettersNoise")]
        [TestCase("__inner __inner__ inner__", @"<strong>inner <strong>inner<\strong> inner<\strong>", TestName = "OverlappingNoise")]
        [TestCase("__", "__", TestName = "SingleDoubleUnderscore")]        
        public void CanRenderStrongTagsTests(string markdown, string expectedHtml)
            => AssertEqualHtml(markdown, expectedHtml);

        [TestCase("_inner __inner__ inner_", @"<em>inner <strong>inner<\strong> inner<\em>", TestName = "StrongInEmShouldWork")]
	    [TestCase("__inner _inner_ inner__", @"<strong>inner <em>inner<\em> inner<\strong>", TestName = "EmInStrongShouldWork")]
        [TestCase("__inner_ _inner__", "__inner_ _ inner__", TestName = "EmAndStrongCollision")]
        public void CanRenderEmAndStrongTagsBothTests(string markdown, string expectedHtml)
	        => AssertEqualHtml(markdown, expectedHtml);

        [TestCase(@"\_inner\_", "_inner_", TestName = "EmTagEscapeSequence")]
	    [TestCase(@"\_\_inner\_\_", "__inner__", TestName = "StrongTagEscapeSequence")]
	    [TestCase(@"\\inner\b", @"\innerb", TestName = "AnotherSymbolEscapeSequence")]
	    public void CanHandleEscapeSequencesTests(string markdown, string expectedHtml)
	        => AssertEqualHtml(markdown, expectedHtml);

        public void AssertEqualHtml(string markdown, string expectedHtml)
            => Renderer.RenderToHtml(markdown).Should().Be(expectedHtml);
    }
}