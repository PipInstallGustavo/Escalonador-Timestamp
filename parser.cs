using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class Parser
{   
    // elementos do in.txt
    public List<string> obj_dados = new List<string>();
    public List<string> transacoes = new List<string>();
    public Dictionary<string, string> timestamps = new Dictionary<string, string>();
    public List<Tuple<string, string>> escalonamentos = new List<Tuple<string, string>>();

    private List<string> timestamps_values = new List<string>();
    private string in_path = "infinal.txt";

    public Parser () {}

    public Parser(List<string> obj_dados, List<string> transacoes, Dictionary<string, string> timestamps, List<Tuple<string, string>> escalonamentos)
    {
        this.obj_dados = obj_dados;
        this.transacoes = transacoes;
        this.timestamps = timestamps;
        this.escalonamentos = escalonamentos;
    }

    public void processar_transacoes_arquivo()
    {
        if (!File.Exists(in_path))
        {
            Console.WriteLine("Arquivo não encontrado!");
            return;
        }

        var lines = File.ReadAllLines(in_path).ToList();

        obj_dados = lines[0]
            .Split(',')
            .Select(s => s.Trim().Replace(";", ""))
            .ToList();

        transacoes = lines[1]
            .Split(',')
            .Select(s => s.Trim().Replace(";", ""))
            .ToList();

        timestamps_values = lines[2]
            .Split(',')
            .Select(s => s.Trim().Replace(";", ""))
            .ToList();

        for (int i = 0; i < transacoes.Count; i++)
        {
            timestamps[transacoes[i]] = timestamps_values[i];
        }

        //processar os escalonamentos
        for (int i = 3; i < lines.Count; i++){
            var partes = lines[i].Split('-');
            string id_escalonamento = partes[0].Trim();
            string operacoes = partes[1].Trim();

            escalonamentos.Add(Tuple.Create(id_escalonamento, operacoes)); //lista de tuplas com o id do escalonamento e as operações de cada um
        }
    }

    //função responsável por criar e escrever o out.txt
    public void escreve_out(List<string> resultados, string outputFilename = "out.txt"){
        using (StreamWriter file = new StreamWriter(outputFilename))
        {
            foreach (string resultado in resultados)
            {
                file.WriteLine(resultado);
            }
        }
    }

}
