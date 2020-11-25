using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PracticeManagerApi.Controllers
{

    [Route("api/version")]
    public class VersionController : ControllerBase
    {
        /// <summary>
        /// バージョンの取得
        /// </summary>
        /// <returns></returns>
        public IActionResult GetVersion()
        {
            var version = new Dictionary<string, string>()
            {
                { "version", "dev"},
            };

            return Ok(version);

        }
    }

        [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
