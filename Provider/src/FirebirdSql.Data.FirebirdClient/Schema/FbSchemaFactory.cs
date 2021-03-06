﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/FirebirdSQL/NETProvider/blob/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$Authors = Carlos Guzman Alvarez, Jiri Cincura (jiri@cincura.net)

using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

using FirebirdSql.Data.FirebirdClient;

namespace FirebirdSql.Data.Schema
{
	internal sealed class FbSchemaFactory
	{
		#region Static Members

		private static readonly string ResourceName = "FirebirdSql.Data.Schema.FbMetaData.xml";

		#endregion

		#region Constructors

		private FbSchemaFactory()
		{
		}

		#endregion

		#region Methods

		public static DataTable GetSchema(FbConnection connection, string collectionName, string[] restrictions)
		{
			var filter = string.Format("CollectionName = '{0}'", collectionName);
			var ds = new DataSet();
			using (var xmlStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
			{
				var oldCulture = Thread.CurrentThread.CurrentCulture;
				try
				{
					Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
					// ReadXml contains error: http://connect.microsoft.com/VisualStudio/feedback/Validation.aspx?FeedbackID=95116
					// that's the reason for temporarily changing culture
					ds.ReadXml(xmlStream);
				}
				finally
				{
					Thread.CurrentThread.CurrentCulture = oldCulture;
				}
			}

			var collection = ds.Tables[DbMetaDataCollectionNames.MetaDataCollections].Select(filter);

			if (collection.Length != 1)
			{
				throw new NotSupportedException("Unsupported collection name.");
			}

			if (restrictions != null && restrictions.Length > (int)collection[0]["NumberOfRestrictions"])
			{
				throw new InvalidOperationException("The number of specified restrictions is not valid.");
			}

			if (ds.Tables[DbMetaDataCollectionNames.Restrictions].Select(filter).Length != (int)collection[0]["NumberOfRestrictions"])
			{
				throw new InvalidOperationException("Incorrect restriction definition.");
			}

			switch (collection[0]["PopulationMechanism"].ToString())
			{
				case "PrepareCollection":
					return PrepareCollection(connection, collectionName, restrictions);

				case "DataTable":
					return ds.Tables[collection[0]["PopulationString"].ToString()].Copy();

				case "SQLCommand":
					return SqlCommandSchema(connection, collectionName, restrictions);

				default:
					throw new NotSupportedException("Unsupported population mechanism");
			}
		}

		#endregion

		#region Private Methods

		private static DataTable PrepareCollection(FbConnection connection, string collectionName, string[] restrictions)
		{
			FbSchema returnSchema = null;

			switch (collectionName.ToUpperInvariant())
			{
				case "CHARACTERSETS":
					returnSchema = new FbCharacterSets();
					break;

				case "CHECKCONSTRAINTS":
					returnSchema = new FbCheckConstraints();
					break;

				case "CHECKCONSTRAINTSBYTABLE":
					returnSchema = new FbChecksByTable();
					break;

				case "COLLATIONS":
					returnSchema = new FbCollations();
					break;

				case "COLUMNS":
					returnSchema = new FbColumns();
					break;

				case "COLUMNPRIVILEGES":
					returnSchema = new FbColumnPrivileges();
					break;

				case "DOMAINS":
					returnSchema = new FbDomains();
					break;

				case "FOREIGNKEYCOLUMNS":
					returnSchema = new FbForeignKeyColumns();
					break;

				case "FOREIGNKEYS":
					returnSchema = new FbForeignKeys();
					break;

				case "FUNCTIONS":
					returnSchema = new FbFunctions();
					break;

				case "GENERATORS":
					returnSchema = new FbGenerators();
					break;

				case "INDEXCOLUMNS":
					returnSchema = new FbIndexColumns();
					break;

				case "INDEXES":
					returnSchema = new FbIndexes();
					break;

				case "PRIMARYKEYS":
					returnSchema = new FbPrimaryKeys();
					break;

				case "PROCEDURES":
					returnSchema = new FbProcedures();
					break;

				case "PROCEDUREPARAMETERS":
					returnSchema = new FbProcedureParameters();
					break;

				case "PROCEDUREPRIVILEGES":
					returnSchema = new FbProcedurePrivilegesSchema();
					break;

				case "ROLES":
					returnSchema = new FbRoles();
					break;

				case "TABLES":
					returnSchema = new FbTables();
					break;

				case "TABLECONSTRAINTS":
					returnSchema = new FbTableConstraints();
					break;

				case "TABLEPRIVILEGES":
					returnSchema = new FbTablePrivileges();
					break;

				case "TRIGGERS":
					returnSchema = new FbTriggers();
					break;

				case "UNIQUEKEYS":
					returnSchema = new FbUniqueKeys();
					break;

				case "VIEWCOLUMNS":
					returnSchema = new FbViewColumns();
					break;

				case "VIEWS":
					returnSchema = new FbViews();
					break;

				case "VIEWPRIVILEGES":
					returnSchema = new FbViewPrivileges();
					break;

				default:
					throw new NotSupportedException("The specified metadata collection is not supported.");
			}

			return returnSchema.GetSchema(connection, collectionName, restrictions);
		}

		private static DataTable SqlCommandSchema(FbConnection connection, string collectionName, string[] restrictions)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
