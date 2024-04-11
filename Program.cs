using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdiviniHunt_PSX
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private bool juegoActivo = false;
        private Dictionary<ulong, int> apuestas = new Dictionary<ulong, int>();
        private int numeroGanador;
        private int numGanadores = 1;


        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);

            //_client = new DiscordSocketClient();
            _commands = new CommandService();

            _client.Log += Log;
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(System.Reflection.Assembly.GetEntryAssembly(), _services);

            // Aquí va tu token
            await _client.LoginAsync(TokenType.Bot, "MTIyNzkxODExMTczOTc0NDI1Ng.GWp_oI.HErAA27lVPjM0KiBpj0bpusZJ0s4QW6rMC6hz0");
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            if (message == null) return;
            var context = new SocketCommandContext(_client, message);
            if (context == null) return;

            if (message.Author.IsBot) return;

            if (message.Content.ToLower().StartsWith("/iniciar"))
            {
                var user = context.User as SocketGuildUser;
                if (user == null || !user.GuildPermissions.Administrator)
                {
                    await context.Channel.SendMessageAsync("Necesitas permisos de administrador para ejecutar este comando.");
                    return;
                }

                var parts = message.Content.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out int ganadores))
                {
                    numGanadores = ganadores;
                }
                else
                {
                    numGanadores = 1;
                }

                juegoActivo = true;
                apuestas.Clear();
                await context.Channel.SendMessageAsync("¡El juego ha comenzado! Envía tu número con /apostar [número].");
            }
            else if (message.Content.ToLower().StartsWith("/finalizar"))
            {
                var user = context.User as SocketGuildUser;
                if (user == null || !user.GuildPermissions.Administrator)
                {
                    await context.Channel.SendMessageAsync("Necesitas permisos de administrador para ejecutar este comando.");
                    return;
                }
                if (!juegoActivo)
                {
                    await context.Channel.SendMessageAsync("No hay un juego activo.");
                    return;
                }

                var parts = message.Content.Split(' ');
                if (parts.Length < 2 || !int.TryParse(parts[1], out numeroGanador))
                {
                    await context.Channel.SendMessageAsync("Por favor, finaliza el juego con un número ganador: /finalizar [número].");
                    return;
                }

                juegoActivo = false;
                EvaluarGanadores(context, numeroGanador);
            }
            else if (message.Content.ToLower().StartsWith("/reiniciar"))
            {
                var user = context.User as SocketGuildUser;
                if (user == null || !user.GuildPermissions.Administrator)
                {
                    await context.Channel.SendMessageAsync("Necesitas permisos de administrador para ejecutar este comando.");
                    return;
                }

                if (!juegoActivo)
                {
                    await context.Channel.SendMessageAsync("No hay un juego activo.");
                    return;
                }

                ReiniciarJuego();

            }
            else if (!string.IsNullOrEmpty(message.Content) && juegoActivo)
            {
                int numMess = 0;

                if (!int.TryParse(message.Content, out numMess))
                {
                    return;
                }

                var userId = context.User.Id;
                if (apuestas.ContainsKey(userId))
                {
                    apuestas[userId] = numMess;
                    return;
                }
                else
                    apuestas.Add(userId, numMess);

            }
        }
        private async void EvaluarGanadores(SocketCommandContext context, int numeroGanador)
        {
            if (apuestas.Count == 0)
            {
                await context.Channel.SendMessageAsync("Nadie ha apostado en este juego.");
                return;
            }

            var ganadores = apuestas.OrderBy(x => Math.Abs(x.Value - numeroGanador)).Take(numGanadores).ToList();

            if (ganadores.Any())
            {
                int numGanador = 1;
                var resultados = new List<string>();
                foreach (var ganador in ganadores)
                {
                    var user = context.Guild.GetUser(ganador.Key);
                    var distancia = Math.Abs(ganador.Value - numeroGanador);

                    if (distancia == 0)
                    {
                        resultados.Add($"----------------------------------------------------------------------------------------------------------");
                        resultados.Add($"{numGanador}- APAGAD LA RADIO, <@{user.Id}>  acertó el número exacto y es un superganador! OUUUUUU!");
                        resultados.Add($"----------------------------------------------------------------------------------------------------------");
                    }
                    else
                    {
                        resultados.Add($"{numGanador}- <@{user.Id}> Ha ganado con {ganador.Value}, cercano al ganador {numeroGanador}.");
                    }
                    numGanador++;
                }
                if (resultados.Last() != $"----------------------------------------------------------------------------------------------------------")
                    resultados.Add($"----------------------------------------------------------------------------------------------------------");

                await context.Channel.SendMessageAsync("Ganadores: \n" + string.Join("\n ", resultados));
            }
            else
            {
                await context.Channel.SendMessageAsync("No hay ganadores esta vez.");
            }

            ReiniciarJuego();
        }

        private void ReiniciarJuego()
        {
            apuestas.Clear();
            juegoActivo = false;
        }
    }
}