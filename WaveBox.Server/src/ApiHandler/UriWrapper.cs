using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaveBox.ApiHandler {
    public class UriWrapper {
        // URI breakdown into parts, a string, and parameters
        public IList<string> UriParts { get; set; }
        public string UriString { get; set; }
        public Dictionary<string, string> Parameters { get; set; }

        // Shortcuts for first and last part of URI
        public string FirstPart { get { return UriPart(0); } }
        public string LastPart { get { return UriPart(UriParts.Count - 1); } }

        // ID parsed from URL in REST form
        public int? Id { get; set; }

        // CRUD operation parsed in REST form
        public string Action { get; set; }

        // Determine if URI contains an API call
        public bool IsApiCall { get { return FirstPart == "api"; } }

        // Determines which API action will be called
        public string ApiAction {
            get {
                try {
                    return IsApiCall ? UriPart(1).ToLower() : null;
                } catch {
                    return null;
                }
            }
        }

        /// <summary>
        /// Constructor for UriWrapper, takes in a URI string and enables methods to parse its pieces
        /// </summary>
        public UriWrapper(string uriString, string httpMethod = null) {
            // Store the original URI in a string
            this.UriString = uriString;

            // Parse parameters in the URI
            this.ParseParameters();

            // Store the parts of the URI in a List of strings
            this.UriParts = RemoveEmptyElements(this.UriString.Split('/'));

            // Set ID null unless a valid one is found
            this.Id = null;

            // Set action to read unless a valid one is found
            this.Action = "read";
            if (this.Parameters.ContainsKey("action")) {
                this.Action = this.Parameters["action"];
            }

            // Check for RESTful HTTP method, and set action accordingly, overriding parameter action
            if (httpMethod != null) {
                // DELETE - destroy a resource
                if (httpMethod == "DELETE") {
                    this.Action = "delete";
                }
                // PUT - update a resource
                if (httpMethod == "PUT") {
                    this.Action = "update";
                }
            }

            // Check for ID passed in REST form (e.g. /api/songs/6)
            // NOTE: try/catch'd to avoid exceptions thrown on web UI loading
            try {
                int id = 0;
                if (Int32.TryParse(this.LastPart, out id)) {
                    // Capture ID to be used in API handlers
                    this.Id = id;
                }
            } catch {
            }
        }

        /// <summary>
        /// Return the element at a given index of the URI
        /// </summary>
        public string UriPart(int index) {
            // Make sure the URI's part count is greater than the index
            if (this.UriParts.Count > index) {
                return this.UriParts.ElementAt(index);
            }

            // Return null if the index was out of range
            return null;
        }

        /// <summary>
        /// Parse parameters into a Dictionary from a URI
        /// </summary>
        private void ParseParameters() {
            // Initialize a dictionary
            this.Parameters = new Dictionary<string, string>();

            // Grab parameters, if they exist
            if (UriString.Contains('?')) {
                // if we split the uri by the question mark, the second part of the split will be the params
                string parametersString = this.UriString.Split('?')[1];
                string[] splitParams = parametersString.Split(new char[] {'=', '&'});

                // Add parameters to the dictionary as we parse the parameters array
                for (int i = 0; i <= splitParams.Length - 2; i = i + 2) {
                    this.Parameters.Add(splitParams[i], splitParams[i + 1]);
                }

                // Store the URI before parameters in the UriString property
                this.UriString = this.UriString.Substring(0, this.UriString.IndexOf('?'));
            }
        }

        /// <summary>
        /// Purge the empty elements in an array of strings, returning a list of strings
        /// </summary>
        private IList<string> RemoveEmptyElements(string[] input) {
            IList<string> result = new List<string>();

            foreach (string s in input) {
                if (s != null && s != "") {
                    result.Add(s);
                }
            }

            return result;
        }
    }
}
