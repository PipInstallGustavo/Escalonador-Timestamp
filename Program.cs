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
        Console.WriteLine("Prints a seguir são do Program.cs");
        //printa as as variaveis

        Console.WriteLine("Objetos de Dados:");
        foreach (var obj in obj_dados)
        {
            Console.WriteLine($" - {obj}");
        }

        Console.WriteLine("Transações:");
        foreach (var trans in transacoes)
        {
            Console.WriteLine($" - {trans}");
        }

        Console.WriteLine("Timestamps:");
        foreach (var ts in timestamps)
        {
            Console.WriteLine($" - {ts.Key}: {ts.Value}");
        }

        Console.WriteLine("Escalonamentos:");
        foreach (var esc in escalonamentos)
        {
            Console.WriteLine($" - {esc.Item1}: {esc.Item2}");
        }

        // A partir de timestamps e escalomanentos, vamos chamar o algoritmo Timestamp-Based Scheduling
        var escalonador = new Escalonador(timestamps, escalonamentos);
        var resultado = escalonador.Executar();
        // Criamos um arquivo out.txt para escrever o resultado
        System.IO.File.WriteAllLines("out.txt", resultado);

    }
}
