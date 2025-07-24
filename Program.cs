using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        // Cria o parser e processa o arquivo
        var parser = new Parser();
        parser.processar_transacoes_arquivo();

        Console.WriteLine("Objetos de Dados:");
        foreach (var obj in parser.obj_dados)
        {
            Console.WriteLine($" - {obj}");
        }

        Console.WriteLine("Transações:");
        foreach (var trans in parser.transacoes)
        {
            Console.WriteLine($" - {trans}");
        }

        Console.WriteLine("Timestamps:");
        foreach (var ts in parser.timestamps)
        {
            Console.WriteLine($" - {ts.Key}: {ts.Value}");
        }

        Console.WriteLine("Escalonamentos:");
        foreach (var esc in parser.escalonamentos)
        {
            Console.WriteLine($" - {esc.Item1}: {esc.Item2}");
        }

        // A partir de timestamps e escalonamentos, vamos executar o algoritmo Timestamp-Based Scheduling
        var escalonador = new Escalonador(parser.timestamps, parser.escalonamentos);
        var resultado = escalonador.Executar();
        
        // Criamos um arquivo out.txt para escrever o resultado
        parser.escreve_out(resultado);
        Console.WriteLine("Resultados escritos em out.txt");
    }
}