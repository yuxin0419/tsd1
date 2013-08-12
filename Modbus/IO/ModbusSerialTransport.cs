using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using log4net;
using Modbus.Message;
using Modbus.Utility;

namespace Modbus.IO
{
	/// <summary>
	/// Transport for Serial protocols.
	/// Refined Abstraction - http://en.wikipedia.org/wiki/Bridge_Pattern
	/// </summary>
	public abstract class ModbusSerialTransport : ModbusTransport
	{
        private  ILog _logger;
		private bool _checkFrame = true;

		internal ModbusSerialTransport(IStreamResource streamResource)
			: base(streamResource)
		{
             _logger= LogManager.GetLogger(this.GetType());
			Debug.Assert(streamResource != null, "Argument streamResource cannot be null.");
		}

		/// <summary>
		/// Gets or sets a value indicating whether LRC/CRC frame checking is performed on messages.
		/// </summary>
		public bool CheckFrame
		{
			get { return _checkFrame; }
			set { _checkFrame = value; }
		}

		internal void DiscardInBuffer()
		{
			StreamResource.DiscardInBuffer();
		}

		internal override void Write(IModbusMessage message)
		{
			DiscardInBuffer();

			byte[] frame = BuildMessageFrame(message);
			_logger.InfoFormat("TX: {0}", StringUtility.Join(", ", frame));
			StreamResource.Write(frame, 0, frame.Length);
		}

		internal override IModbusMessage CreateResponse<T>(byte[] frame)
		{
			IModbusMessage response = base.CreateResponse<T>(frame);

			// compare checksum
			if (CheckFrame && !ChecksumsMatch(response, frame))
			{
				string errorMessage = String.Format(CultureInfo.InvariantCulture, "Checksums failed to match {0} != {1}", StringUtility.Join(", ", response.MessageFrame), StringUtility.Join(", ", frame));
				_logger.Error(errorMessage);
				throw new IOException(errorMessage);
			}

			return response;
		}

		internal abstract bool ChecksumsMatch(IModbusMessage message, byte[] messageFrame);

        internal override void OnValidateResponse(IModbusMessage request, IModbusMessage response)
        {
            // no-op
        }
    }
}
