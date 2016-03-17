#region Header
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
//using Sandbox.Common.ObjectBuilders;
//using VRageMath;
//using VRage;

namespace SpaceEngineersScripting
{
	public class Wrapper
	{
		static void Main()
		{
			new CodeEditorEmulator().Main ("");
		}
	}


	class CodeEditorEmulator : Sandbox.ModAPI.Ingame.MyGridProgram
	{
		#endregion
		#region CodeEditor
		//Configuration
		//--------------------


		//Definitions
		//--------------------


		//Types
		//--------------------
		public struct Bus
		{
			//The bus stores a series of records as a string, each ended by the terminator
			//	*It is not checked that records do not contain the terminator
			//	 so make sure that you do not corrupt it by using this character

			//Records have one of two Record Types:
			//-Static: non-volatile; read or overwritten only
			//-Temporary: appended on write (duplicates allowed), destructive read (FIFO)
			//e.g. use static records for public status/exported data
			//e.g. use temporary records to issue commands / directional data transfer

			//Records have an Id allowing basic discrimination
			//(e.g. source and/or destination and/or data as needed)

			//Additionally, records all have a Data Type to allow for basic type checking.
			//Different types make a record unique, even if they share an Id.
			//(new types may be easily added, so long as they can be encoded/decoded
			// from a string, and ensured not to contain the record terminator)

			//Static records are stored at the head of the store
			//Temporary records are appended to the end of the store
			//(this simplifies searching)

			//FORMAT
			//<Record> ::= <RecordBody><recordTerminator>
			//<RecordBody> ::= <RecordStatic> | <RecordTemporary>
			//<RecordStatic> ::= <recordTypeStatic><id><Data>
			//<RecordTemporary> ::= <recordTypeTemporary><id><Data>
			//<id> ::= <string-lengthId>
			//<Data> ::= <DataInt> | <DataFloat> | <DataString>
			//<DataInt> ::= <dataTypeInt><int>
			//<DataFloat> ::= <dataTypeFloat><float>
			//<DataString> ::= <dataTypeFloat><string>

			//e.g. Static float record 'Altitude' = 100.0f
			//SAltitude________F100\n
			//e.g. Temporary string record 'Cmd.Clock' = "reset"
			//TCmd.Clock_______Sreset\n


			//Configuration
			const char
				recordTerminator = '\n';//'\x1E'; //Record separator

			const char
				recordTypeStatic = 'S',
				recordTypeTemp = 'T';
			const char
				dataTypeInt = 'I',
				dataTypeFloat = 'F',
				dataTypeString = 'S';

			const int
				lengthId = 16;

			//The source of the storage
			public IMyTextPanel bus;

			//Internal storage interface
			private string Store{
				get { return bus.GetPublicText(); }
				set { bus.WritePublicText(value, false); }
			}
			private void Append(string value){
				bus.WritePublicText(value, true);
			}


			//Utility definitions
			private const int
				offsetStaticData = lengthId +2; //recordType +id +dataType


			//Internal Implementation

			/// <summary>
			/// Finds the static record meeting all of the requirements.
			/// </summary>
			/// <returns>The index of the start of the static record.</returns>
			/// <param name="store">The storage string to search in.</param>
			/// <param name="id">The record id to match.</param>
			/// <param name="dataType">The record data type constant to match.</param>
			/// <param name="startIndex">The index to advance from.</param>
			/// <remarks>
			/// This procedure assumes the data structure of <paramref name="storage"/>
			/// is correct; no checks are performed when trying to access it.
			/// </remarks>
			private int FindStaticRecord(ref string store, ref string id, char dataType, int startIndex=0){
				//Find the index of the start of the record matching id and dataType
				//-otherwise return -1
				int i = startIndex;
				while (i < store.Length) {
					//Examine the current record
					//-log the start index in case we need to return in
					//-check that we are still examining static records
					//  >otherwise stop searching
					//-check that the ids match
					//-check that data types match
					//-if they match, return the start index
					//  >otherwise, skip data and continue with the next record
					int indexRecordStart = i;
					//check record type
					if (store[i++] != recordTypeStatic) {
						//static records should all be at the beginning
						//stop if we encounter another type
						return -1;
					} else {
						//compare ids (without allocating more memory for strings)
						bool matchId = true;
						for (int ii=0; ii<lengthId; ii++) {
							if (store[i++] != id[ii]) {
								matchId = false;
								break;
							}
						}
						//check ids and data type
						if (matchId && store[i++] == dataType) {
							//record type, id and data type all match this record
							return indexRecordStart;
						} else {
							//move on to next record
							while (store[i++] != recordTerminator) {};
						}
					}

				}
				//if we run out of records, it was not found
				return -1;
			}


			/// <summary>
			/// Returns a copy of the raw Data section of the given static record.
			/// </summary>
			/// <returns>The raw Data section of the static record referenced</returns>
			/// <param name="store">The storage string to extract from.</param>
			/// <param name="indexRecordStart">The index the record starts at.</param>
			/// <remarks>
			/// This procedure assumes the data structure of <paramref name="storage"/>
			/// is correct; no checks are performed when trying to access it.
			/// </remarks>
			private string ExtractStaticData(ref string store, int indexRecordStart){
				int
				indexDataStart = indexRecordStart +offsetStaticData,
				indexDataEnd = indexDataStart;
				//Data segment continues until the end of the record
				while (store[indexDataEnd] != recordTerminator)
					indexDataEnd++;
				return store.Substring(indexDataStart, indexDataEnd -indexDataStart);
			}


			/// <summary>
			/// WriteStatic archetype; overwrite/add raw string as a static record.
			/// </summary>
			private void WriteStaticData(ref string id, char dataType, ref string data){
				string
					store = Store;
				int
					indexRecordStart = FindStaticRecord(ref store, ref id, dataType);

				if (indexRecordStart < 0) {
					//the record was not found
					//add the record to the start
					Store =
						recordTypeStatic +id +dataType +data +recordTerminator
						+store;
				} else {
					//overwrite the existing record in-place
					//-find the start of the next record
					//-insert new record between end of previous record and next record
					int indexRecordNext = indexRecordStart;
					while (store[indexRecordNext++] != recordTerminator) {};
					Store =
						store.Substring (0, indexRecordStart)
						+recordTypeStatic +id +dataType +data +recordTerminator
						+store.Substring (indexRecordNext, store.Length -indexRecordNext);
				}
			}


			/// <summary>
			/// ReadStatic archetype; finds and returns the raw string from a static record.
			/// </summary>
			/// <returns>true if the record was found, otherwise false</returns>
			private bool ReadStaticData(ref string id, char dataType, out string data){
				string
					store = Store;
				int
					indexRecordStart = FindStaticRecord(ref store, ref id, dataType);

				if (indexRecordStart < 0) {
					//the record was not found
					data = null;
					return false;
				} else {
					data = ExtractStaticData(ref store, indexRecordStart);
					return true;
				}
			}


			//PUBLIC INTERFACE

			public Bus(IMyTextPanel bus){
				this.bus = bus;
			}


			public static string extendId(string id){
				return id.PadRight(lengthId, ' ');
			}


			public void WriteStaticInt(string id, int value){
				string data = value.ToString ();
				WriteStaticData(ref id, dataTypeInt, ref data);
			}

			public bool ReadStaticInt(string id, out int value){
				string data;
				if ( ReadStaticData(ref id, dataTypeInt, out data) ) {
					//Record found
					//Validity depends on ability to parse the data
					return int.TryParse(data, out value);
				} else {
					//The record could not be found
					value = 0;
					return false;
				}
			}


			public void WriteStaticFloat(string id, float value){
				string data = value.ToString ("R");
				WriteStaticData(ref id, dataTypeFloat, ref data);
			}

			public bool ReadStaticFloat(string id, out float value){
				string data;
				if ( ReadStaticData(ref id, dataTypeFloat, out data) ) {
					//Record found
					//Validity depends on ability to parse the data
					return float.TryParse(data, out value);
				} else {
					//The record could not be found
					value = float.NaN;
					return false;
				}
			}


			public void WriteStaticString(string id, string value){
				WriteStaticData(ref id, dataTypeString, ref value);
			}

			public bool ReadStaticString(string id, out string value){
				if ( ReadStaticData(ref id, dataTypeString, out value) ) {
					//Record found
					return true;
				} else {
					//The record could not be found
					value = null;
					return false;
				}
			}

		}


		//Global variables
		//--------------------
		bool restarted = true;

		Bus bus;

		readonly string
			aId = Bus.extendId ("a"),
			bId = Bus.extendId ("b");

		float
			a;


		//Program
		//--------------------
		public void Main(string argument)
		{
			if (restarted){
				bus = new Bus( (IMyTextPanel)GridTerminalSystem.GetBlockWithName("Text Panel") );
				restarted = false;

				if ( !bus.ReadStaticFloat (aId, out a) ) {
					a = 0.25f;
				};
			}

			a *= 2.0f;
			bus.WriteStaticFloat (aId, a);

			bus.WriteStaticFloat(bId, (float)Math.Sqrt(a) );

		}
		#endregion
		#region footer
	}
}
#endregion