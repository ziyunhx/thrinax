using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrinax.NLP
{
    public class Recognition
    {
        /// <summary>
        /// Recog the people.
        /// </summary>
        /// <param name="paper">the paper or splitter words.</param>
        /// <param name="isSplitter">is splitter?</param>
        /// <returns>the list of people name.</returns>
        public static List<string> RecogPeople(string paper, bool isSplitter = false)
        {
            if (string.IsNullOrEmpty(paper))
                return null;

            //if (!isSplitter)
            //    paper = WordSplitter.Splite(paper, true);

            string[] words = paper.Split(' ');

            if (words == null || words.Length < 1)
                return null;

            List<string> result = new List<string>();
            result.AddRange(words.Where(f => f.EndsWith("/nr") || f.EndsWith("/nrf")).Select(s => s.Substring(0, s.IndexOf('/'))));

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Recog the region.
        /// </summary>
        /// <param name="paper">the paper or splitter words.</param>
        /// <param name="isSplitter">is splitter?</param>
        /// <returns>the list of region name.</returns>
        public static List<string> RecogRegion(string paper, bool isSplitter = false)
        {
            if (string.IsNullOrEmpty(paper))
                return null;

            //if (!isSplitter)
            //    paper = WordSplitter.Splite(paper, true);

            string[] words = paper.Split(' ');

            if (words == null || words.Length < 1)
                return null;

            List<string> result = new List<string>();
            result.AddRange(words.Where(f => f.EndsWith("/ns")).Select(s => s.Substring(0, s.IndexOf('/')).TrimEnd('省').TrimEnd('市')));

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Recog the organization.
        /// </summary>
        /// <param name="paper">the paper or splitter words.</param>
        /// <param name="isSplitter">is splitter?</param>
        /// <returns>the list of organization name.</returns>
        public static List<string> RecogOrganization(string paper, bool isSplitter = false)
        {
            if (string.IsNullOrEmpty(paper))
                return null;

            //if (!isSplitter)
            //    paper = WordSplitter.Splite(paper, true);

            string[] words = paper.Split(' ');

            if (words == null || words.Length < 1)
                return null;

            List<string> result = new List<string>();
            result.AddRange(words.Where(f => f.EndsWith("/nt")).Select(s => s.Substring(0, s.IndexOf('/'))));

            return result.Distinct().ToList();
        }
    }
}
