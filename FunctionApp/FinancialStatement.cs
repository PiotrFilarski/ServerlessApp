using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FunctionApp
{
    class FinancialStatement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public int Income { get; set; }
        public int Age { get; set; }
        public byte[] Photo { get; set; }
        public byte[] Code { get; set; }
    }
}
