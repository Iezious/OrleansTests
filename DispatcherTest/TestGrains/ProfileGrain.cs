using System;
using System.Threading.Tasks;
using Orleans;

namespace TestGrains
{
    public interface IProfileGrain : IGrainWithStringKey
    {
        Task AddLog();
    }

    public class ProfileGrain : Grain<ProfileData>, IProfileGrain
    {

        protected override Task WriteStateAsync()
        {
            return new DataDriver().UpdateProfile(this.GetPrimaryKeyString(), State.LogsCount);
        }

        protected override async Task ReadStateAsync()
        {
            State = await new DataDriver().GetProfile(this.GetPrimaryKeyString()) 
                ?? new ProfileData() { Id = this.GetPrimaryKeyString(), LogsCount = 0, Name = this.GetPrimaryKeyString(), DName = ""};
        }

        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            await ReadStateAsync();
        }

        public async Task AddLog()
        {
            State.LogsCount++;
            Console.WriteLine($"{this.State.Name} : {this.State.LogsCount}");
            await WriteStateAsync();
        }
    }
}
