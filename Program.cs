using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var obj_dados = new List<string>();
        var transacoes = new List<string>();
        var timestamps = new Dictionary<string, string>();
        var escalonamentos = new List<Tuple<string, string>>();

        var parser = new Parser(obj_dados, transacoes, timestamps, escalonamentos);
        parser.processar_transacoes_arquivo();
    }
}
