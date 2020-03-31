namespace producer_consumer
{
    using System;
    using System.Threading.Channels;
    using System.Threading.Tasks;


    class Producer<T>
    {

        private readonly ChannelWriter<T> _channelWriter;
        private int _delay;
        private readonly Func<T> _fakeValueProducer;

        public Producer(ChannelWriter<T> channelWriter, int delay, Func<T> fakeValueProducer)
        {
            _channelWriter = channelWriter;
            _delay = delay;
            _fakeValueProducer = fakeValueProducer;
        }


        public async Task Start()
        {
            Console.WriteLine("Starting producer : {0}", DateTime.UtcNow.ToString());
            for (var i = 0; i < 10; i++)
            {
                // simulate the production
                //await Task.Delay(_delay);

                await _channelWriter.WriteAsync(_fakeValueProducer());
            }
            Console.WriteLine("Producer has finished!");
        }
    }

    class Consumer<T>
    {
        private readonly ChannelReader<T> _channelReader;

        public Consumer(ChannelReader<T> channelReader)
        {
            _channelReader = channelReader;
        }

        public async Task ProcessData()
        {
            while (await _channelReader.WaitToReadAsync())
            {
                var result = await _channelReader.ReadAsync();
                Console.WriteLine("Data read:{0}", result.ToString());
            }

        }
    }

    class Program
    {
        async static Task Main(string[] args)
        {
            await ChannelSimpleExample();
            Console.WriteLine("Hello World!");
        }

        static async Task ChannelSimpleExample()
        {
            var channel = Channel.CreateBounded<int>(4);
            var (reader, writer) = (channel.Reader, channel.Writer);
            var rdm = new Random();
            var producer = new Producer<int>(writer, 10, () =>
            {
                var result = rdm.Next();
                Console.WriteLine("Production1 value : {0}", result);
                return result;
            });

            var producer2 = new Producer<int>(writer, 10, () =>
            {
                var result = rdm.Next(50);
                Console.WriteLine("Production2 value : {0}", result);
                return result;
            });
            var cosumer = new Consumer<int>(reader);
            var cosumerTask = cosumer.ProcessData();

            var task1 = producer.Start();
            var task2 = producer2.Start();

            await Task.WhenAll(task1, task2)
            .ContinueWith(_ => channel.Writer.Complete());

            await cosumerTask;
        }
    }
}
